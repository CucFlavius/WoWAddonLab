using System.Security.Cryptography;
using System.Text;
using WoWAddonLab.Addons;
using WoWAddonLab.Configuration;

namespace WoWAddonLab.Configuration;

public sealed record SavedVariablesProfile(
    string Id,
    string DisplayName,
    string? AccountName,
    string? RealmName,
    string? CharacterName,
    string? AccountSourceDirectory,
    string? CharacterSourceDirectory);
