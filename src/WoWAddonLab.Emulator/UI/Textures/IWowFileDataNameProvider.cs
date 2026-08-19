using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public interface IWowFileDataNameProvider
{
    bool TryGetFilename(uint fileDataId, out string filename);
}
