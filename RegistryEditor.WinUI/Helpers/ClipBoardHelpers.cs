using Windows.ApplicationModel.DataTransfer;

namespace RegistryValley.App.Helpers
{
    public static class ClipBoardHelpers
    {
        public static void SetContent(string str)
        {
            var dp = new DataPackage();
            dp.SetText(str);
            Clipboard.SetContent(dp);
        }
    }
}
