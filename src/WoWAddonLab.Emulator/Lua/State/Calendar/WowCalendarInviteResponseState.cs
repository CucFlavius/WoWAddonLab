using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarInviteResponseState(
    WowCalendarEventIndexState Event,
    byte Response,
    bool UsesSignUpPacket);
