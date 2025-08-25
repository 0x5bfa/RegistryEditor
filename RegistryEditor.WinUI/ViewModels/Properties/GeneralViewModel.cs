using Microsoft.UI.Xaml;
using RegistryValley.App.Models;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace RegistryValley.App.ViewModels.Properties
{
    public class GeneralViewModel : ObservableObject
    {
        public GeneralViewModel()
        {
        }

        #region Fields and Properties
        private KeyItem _keyItem;
        public KeyItem KeyItem { get => _keyItem; set => SetProperty(ref _keyItem, value); }
        #endregion
    }
}
