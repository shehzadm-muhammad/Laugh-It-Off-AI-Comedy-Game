using UnityEngine;

public static class GameState
{
    public static string SelectedCharacter;
    public static string SelectedScene;
    public static string SelectedMood;

    public static string LastSummary;
    public static string LastRating = "It Was Okay";
    public static string LastFunnyLine;
    public static string LastBestLine = "";
    public static int LastTurnCount;

    public static string LastSessionId;
}