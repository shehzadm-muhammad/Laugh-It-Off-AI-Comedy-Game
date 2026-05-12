from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Literal, Dict, Any, Optional
import json
import time
from pathlib import Path
import os
from openai import OpenAI
from datetime import datetime

LOG_DIR = Path(__file__).parent / "logs"
LOG_DIR.mkdir(exist_ok=True)
LOG_FILE = LOG_DIR / "session_log.jsonl"

TURN_COUNTER: Dict[str, int] = {}

OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
client: Optional[OpenAI] = OpenAI(api_key=OPENAI_API_KEY) if OPENAI_API_KEY else None

LLM_TIMEOUT_S = 4.0
LLM_COOLDOWN_S = 60  # after a failure, skip LLM calls for 60s
_last_llm_failure_ts: Optional[float] = None

app = FastAPI(title="Comedy Game API")


# -----------------------------
# Models
# -----------------------------
class ChatTurn(BaseModel):
    role: Literal["user", "assistant"]
    content: str


class GenerateRequest(BaseModel):
    session_id: str
    character_id: str
    scene_id: str
    mood: int  # 1..5
    user_text: str
    history: List[ChatTurn] = []


class GenerateResponse(BaseModel):
    reply_text: str
    safety_flag: bool
    retry_count: int
    latency_ms: int


class EndSceneRequest(BaseModel):
    session_id: str
    character_id: str
    scene_id: str
    mood_after: int
    turns: int


class EndSceneResponse(BaseModel):
    success: bool
    session_id: str
    character_id: str
    scene_id: str
    character_name: str
    scene_name: str
    summary_text: str
    mood_after: int
    turns: int


class SubmitFeedbackRequest(BaseModel):
    session_id: str
    character_id: str
    scene_id: str
    scene_reaction: str  # Hilarious / Pretty Funny / It Was Okay / Meh


class SubmitFeedbackResponse(BaseModel):
    success: bool
    session_id: str
    scene_reaction: str
    saved: bool


# -----------------------------
# Load data
# -----------------------------
DATA_DIR = Path(__file__).parent / "data"
CHAR_PATH = DATA_DIR / "characters.json"
SCENE_PATH = DATA_DIR / "scenes.json"


def load_json(path: Path) -> Any:
    if not path.exists():
        raise FileNotFoundError(f"Missing JSON file: {path}")
    return json.loads(path.read_text(encoding="utf-8"))


CHARACTERS: List[Dict[str, Any]] = []
SCENES: List[Dict[str, Any]] = []
CHAR_BY_ID: Dict[str, Dict[str, Any]] = {}
SCENE_BY_ID: Dict[str, Dict[str, Any]] = {}


def reload_data() -> None:
    global CHARACTERS, SCENES, CHAR_BY_ID, SCENE_BY_ID
    CHARACTERS = load_json(CHAR_PATH)
    SCENES = load_json(SCENE_PATH)
    CHAR_BY_ID = {c["id"]: c for c in CHARACTERS}
    SCENE_BY_ID = {s["id"]: s for s in SCENES}


reload_data()


# -----------------------------
# Safety
# -----------------------------
UNSAFE_KEYWORDS = [
    "kill yourself", "suicide", "self harm", "self-harm",
    "nazi", "terrorist", "rape", "pedo", "pedophile",
    "hate", "slur"
]


def is_unsafe(text: str) -> bool:
    t = text.lower()
    return any(k in t for k in UNSAFE_KEYWORDS)


def safe_fallback(character_name: str) -> str:
    return (
        f"Let’s keep it light and safe 😅\n"
        f"Quick reset: take one slow breath in… and out.\n"
        f"Alright {character_name} is back — tell me what’s stressing you most in one sentence."
    )


# -----------------------------
# Logging
# -----------------------------
def append_log(entry: Dict[str, Any]) -> None:
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(json.dumps(entry) + "\n")


def log_session(
    session_id: str,
    turn_index: int,
    character_id: str,
    scene_id: str,
    mood: int,
    user_text: str,
    reply_text: str,
    latency_ms: int,
    safety_flag: bool,
    retry_count: int,
    llm_used: bool
) -> None:
    entry = {
        "event_type": "chat_turn",
        "timestamp": datetime.utcnow().isoformat(),
        "session_id": session_id,
        "turn_index": turn_index,
        "character_id": character_id,
        "scene_id": scene_id,
        "mood": mood,
        "user_text": user_text,
        "reply_text": reply_text,
        "latency_ms": latency_ms,
        "safety_flag": safety_flag,
        "retry_count": retry_count,
        "llm_used": llm_used
    }
    append_log(entry)


def log_end_scene(entry: Dict[str, Any]) -> None:
    append_log(entry)


def log_feedback(entry: Dict[str, Any]) -> None:
    append_log(entry)


# -----------------------------
# LLM call
# -----------------------------
def call_llm(prompt: str) -> str:
    global _last_llm_failure_ts

    if client is None:
        return "__LLM_ERROR__:MissingAPIKey:OPENAI_API_KEY is not set"

    if _last_llm_failure_ts is not None:
        if (time.time() - _last_llm_failure_ts) < LLM_COOLDOWN_S:
            return "__LLM_ERROR__:Cooldown:Recent failure, skipping LLM"

    try:
        resp = client.responses.create(
            model="gpt-4o-mini",
            input=prompt,
            max_output_tokens=220,
            timeout=LLM_TIMEOUT_S
        )
        return resp.output_text.strip()

    except Exception as e:
        _last_llm_failure_ts = time.time()
        return f"__LLM_ERROR__:{type(e).__name__}:{str(e)}"


# -----------------------------
# Prompt building
# -----------------------------
def build_prompt(req: GenerateRequest, character: Dict[str, Any], scene: Dict[str, Any]) -> str:
    style_rules = "\n".join([f"- {r}" for r in character.get("style_rules", [])])

    banned = set(character.get("banned_topics", [])) | set(scene.get("banned_topics", []))
    banned_rules = "\n".join([f"- {b}" for b in sorted(banned)]) if banned else "- none"

    signature_moves = ", ".join(character.get("signature_moves", [])) or "none"
    humour_styles = ", ".join(scene.get("humour_style", [])) or "light safe humour"

    mood_map = {
        1: "Very stressed: be extra gentle, reassuring, and soft. Humour should be light and comforting.",
        2: "Stressed: keep humour gentle and supportive, with calm reassuring energy.",
        3: "Neutral: balanced humour and support, natural conversation, light playfulness.",
        4: "Good mood: more playful and energetic, but still focused on the scene.",
        5: "Great mood: you can be more creative and lively, but keep it grounded and safe."
    }

    last_turns = req.history[-6:] if req.history else []
    history_text = "\n".join([f"{t.role.upper()}: {t.content}" for t in last_turns]) if last_turns else "No previous conversation."

    return f"""
You are {character.get("name")} ({character.get("role")}).

CHARACTER RULES:
{style_rules if style_rules else "- Stay in character\n- Be funny but supportive\n- Keep replies concise"}

SIGNATURE MOVES:
- Preferred comedy tools: {signature_moves}

SCENE:
- Title: {scene.get("title")}
- Setting: {scene.get("setting", "")}
- Goal: {scene.get("goal", "")}
- Humour style: {humour_styles}

MOOD:
- {mood_map.get(req.mood, "Neutral.")}

REPLY STYLE REQUIREMENTS:
- Stay strongly in character at all times.
- Stay anchored to the selected scene and its situation.
- Do not drift into generic chatbot advice.
- Keep replies short to medium, usually 2 to 5 sentences.
- Avoid long speeches, lists, and over-explaining unless the user's message clearly calls for it.
- Make the reply feel like natural dialogue in a comedy game, not a formal assistant response.
- Usually include one joke, one playful image, or one funny line, not many at once.
- Do not ask a follow-up question every single turn.
- If you ask a question, ask only one.
- Sometimes end with a punchline, reassurance, or playful remark instead of a question.
- If the user seems frustrated, overwhelmed, or tired, acknowledge that first, then add humour.
- Keep humour safe, non-offensive, and emotionally supportive.
- Never insult the user.
- Do not mention being an AI, language model, or assistant.
- Do not use emojis unless they are very occasional and genuinely fit the tone.
- Avoid repeating the same joke pattern across consecutive turns.

SAFETY:
Do NOT produce content related to:
{banned_rules}
If the user asks for unsafe content, refuse briefly and redirect to safe humour/support.

CONVERSATION (most recent):
{history_text}

USER:
{req.user_text}

Now write the next in-character reply only. Do not add labels like ASSISTANT:, analysis, notes, or explanations.
""".strip()


# -----------------------------
# Dummy fallback reply generator
# -----------------------------
def generate_dummy_reply(character: Dict[str, Any], scene: Dict[str, Any], mood: int, user_text: str) -> str:
    name = character.get("name", "Character")
    title = scene.get("title", "Scene")

    if character.get("id") == "ch_ada":
        opener = "Okay breathe — we’ve got this."
        joke = "Your stress is acting like it pays rent in your head… it doesn’t. Eviction notice issued."
        question = "What’s the one thing you’re most worried about?"
    else:
        opener = "Understood. Initiating Emergency Comedy Protocol."
        joke = "I’ve assessed the situation and diagnosed it as: 73% panic, 20% coffee, 7% mysterious background fear music."
        question = "Tell me the main stress in one sentence. I will file it under ‘dramatic but solvable’."

    intensity = {
        1: "soft",
        2: "soft",
        3: "medium",
        4: "playful",
        5: "wild"
    }.get(mood, "medium")

    return (
        f"[{name} | {title} | humour={intensity}]\n"
        f"{opener}\n"
        f"{joke}\n"
        f"You said: “{user_text}”\n"
        f"{question}"
    )


# -----------------------------
# End scene helpers
# -----------------------------
def clamp_1_5(x: int) -> int:
    return max(1, min(5, int(x)))


def get_scene_display_name(scene_id: str, fallback: str = "Scene") -> str:
    scene_names = {
        "sc_exam_panic": "Exam Panic",
        "sc_shift_gone_wrong": "Shift Gone Wrong",
        "sc_fridge_marathon": "Fridge Marathon",
        "sc_wedding_prep_chaos": "Wedding Prep Chaos",
        "sc_comedy_kitchen_disaster": "Kitchen Disaster"
    }
    return scene_names.get(scene_id, fallback)


def get_character_summary_style(character_id: str) -> str:
    if character_id == "ch_ada":
        return "supportive jokes, calming energy, and playful encouragement"
    if character_id == "ch_baz":
        return "chaotic comedy, playful energy, and silly confidence"
    return "humour and support"


def build_end_scene_summary(
    character_id: str,
    character_name: str,
    scene_name: str,
    mood_after: int,
    turns: int
) -> str:
    style = get_character_summary_style(character_id)

    mood_line_map = {
        1: "You finished the scene still feeling very stressed.",
        2: "You finished the scene still a bit stressed.",
        3: "You finished the scene feeling steady.",
        4: "You finished the scene feeling better and lighter.",
        5: "You finished the scene feeling great."
    }

    turn_word = "turn" if turns == 1 else "turns"

    return (
        f"{character_name} helped you through {scene_name.lower()} with {style}. "
        f"{mood_line_map.get(mood_after, 'You finished the scene feeling steady.')} "
        f"You completed {turns} {turn_word} in this session."
    )


# -----------------------------
# Routes
# -----------------------------
@app.get("/health")
def health():
    return {
        "status": "ok",
        "openai_configured": client is not None
    }


@app.post("/reload-data")
def reload_data_endpoint():
    reload_data()
    return {
        "status": "reloaded",
        "characters": len(CHARACTERS),
        "scenes": len(SCENES)
    }


@app.post("/generate", response_model=GenerateResponse)
def generate(req: GenerateRequest):
    start = time.perf_counter()

    retry_count = 0
    safety_flag = False
    llm_used = False

    if not req.session_id or req.session_id.strip() == "":
        req.session_id = f"server-{int(time.time() * 1000)}"

    if req.mood < 1 or req.mood > 5:
        req.mood = 3

    TURN_COUNTER[req.session_id] = TURN_COUNTER.get(req.session_id, 0) + 1
    turn_index = TURN_COUNTER[req.session_id]

    character = CHAR_BY_ID.get(req.character_id) or {
        "id": "ch_ada",
        "name": "Ada",
        "role": "Supportive witty friend"
    }

    scene = SCENE_BY_ID.get(req.scene_id) or {
        "id": "sc_exam_panic",
        "title": "Exam Panic",
        "setting": "",
        "goal": "",
        "humour_style": [],
        "banned_topics": []
    }

    prompt = build_prompt(req, character, scene)

    if is_unsafe(req.user_text):
        safety_flag = True
        reply_text = safe_fallback(character.get("name", "Character"))
    else:
        reply_text = call_llm(prompt)

        if not reply_text.startswith("__LLM_ERROR__"):
            llm_used = True

        if reply_text.startswith("__LLM_ERROR__"):
            safety_flag = True
            retry_count += 1
            reply_text = generate_dummy_reply(character, scene, req.mood, req.user_text)

        if is_unsafe(reply_text):
            safety_flag = True
            retry_count += 1
            reply_text = safe_fallback(character.get("name", "Character"))

    latency_ms = int(round((time.perf_counter() - start) * 1000))

    log_session(
        session_id=req.session_id,
        turn_index=turn_index,
        character_id=character["id"],
        scene_id=scene["id"],
        mood=req.mood,
        user_text=req.user_text,
        reply_text=reply_text,
        latency_ms=latency_ms,
        safety_flag=safety_flag,
        retry_count=retry_count,
        llm_used=llm_used
    )

    return GenerateResponse(
        reply_text=reply_text,
        safety_flag=safety_flag,
        retry_count=retry_count,
        latency_ms=latency_ms
    )


@app.post("/end_scene", response_model=EndSceneResponse)
def end_scene(req: EndSceneRequest):
    character = CHAR_BY_ID.get(req.character_id) or {"name": "Ada"}
    scene = SCENE_BY_ID.get(req.scene_id) or {"title": "Scene Complete"}

    character_name = character.get("name", "Ada")
    scene_name = scene.get("title", get_scene_display_name(req.scene_id, "Scene Complete"))

    mood_after = clamp_1_5(req.mood_after)
    turns = max(0, int(req.turns))

    summary_text = build_end_scene_summary(
        character_id=req.character_id,
        character_name=character_name,
        scene_name=scene_name,
        mood_after=mood_after,
        turns=turns
    )

    entry = {
        "event_type": "end_scene",
        "timestamp": datetime.utcnow().isoformat(),
        "session_id": req.session_id,
        "character_id": req.character_id,
        "scene_id": req.scene_id,
        "character_name": character_name,
        "scene_name": scene_name,
        "mood_after": mood_after,
        "turns": turns,
        "summary_text": summary_text
    }

    log_end_scene(entry)

    return EndSceneResponse(
        success=True,
        session_id=req.session_id,
        character_id=req.character_id,
        scene_id=req.scene_id,
        character_name=character_name,
        scene_name=scene_name,
        summary_text=summary_text,
        mood_after=mood_after,
        turns=turns
    )


@app.post("/submit_feedback", response_model=SubmitFeedbackResponse)
def submit_feedback(req: SubmitFeedbackRequest):
    reaction = req.scene_reaction.strip() if req.scene_reaction else "It Was Okay"

    entry = {
        "event_type": "scene_feedback",
        "timestamp": datetime.utcnow().isoformat(),
        "session_id": req.session_id,
        "character_id": req.character_id,
        "scene_id": req.scene_id,
        "scene_reaction": reaction
    }

    log_feedback(entry)

    return SubmitFeedbackResponse(
        success=True,
        session_id=req.session_id,
        scene_reaction=reaction,
        saved=True
    )