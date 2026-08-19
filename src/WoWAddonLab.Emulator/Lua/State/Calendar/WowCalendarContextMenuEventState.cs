using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarContextMenuEventState(
    bool CanComplain,
    bool CanEdit,
    bool CanRemove,
    string? CalendarType,
    bool CanSignUp,
    bool CanRespondToInvite = false,
    bool CanRemoveInvite = false,
    bool CanTentative = false,
    bool TentativeUsesSignUpPacket = false);
