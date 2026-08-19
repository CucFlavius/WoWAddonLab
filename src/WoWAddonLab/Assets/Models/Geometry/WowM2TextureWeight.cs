using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal sealed record WowM2TextureWeight(
    WowModelAnimationTrack<float>? Value);
