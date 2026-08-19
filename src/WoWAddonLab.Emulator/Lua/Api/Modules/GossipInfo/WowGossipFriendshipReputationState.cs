using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGossipFriendshipReputationState(
    int FriendshipFactionId,
    int Standing,
    int MaxRep,
    string? Name,
    string Text,
    int Texture,
    string Reaction,
    int ReactionThreshold,
    int? NextThreshold,
    bool ReversedColor,
    int? OverrideColor);
