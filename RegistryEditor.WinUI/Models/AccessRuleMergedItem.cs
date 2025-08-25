namespace RegistryValley.App.Models
{
    public class AccessRuleMergedItem : ObservableObject
    {
        public REGSAM MaskAllowed { get; set; }
        public REGSAM MaskDenied { get; set; }

        private bool _isInheritedDeniedMask;
        public bool IsInheritedDeniedMask { get => _isInheritedDeniedMask; set => SetProperty(ref _isInheritedDeniedMask, value); }

        private bool _isInheritedAllowedMask;
        public bool IsInheritedAllowedMask { get => _isInheritedAllowedMask; set => SetProperty(ref _isInheritedAllowedMask, value); }

        #region Permission Properties
        public bool AllowFullControl
        {
            get => MaskAllowed.HasFlag(REGSAM.KEY_ALL_ACCESS);
            set => ToggleFlag(REGSAM.KEY_ALL_ACCESS, true, value);
        }

        public bool AllowRead
        {
            get => MaskAllowed.HasFlag(REGSAM.KEY_READ);
            set => ToggleFlag(REGSAM.KEY_READ, true, value);
        }

        public bool AllowSpecialPermissions // TODO
        {
            get => false;
            set => ToggleFlag(REGSAM.DELETE, true, value);
        }

        public bool DenyFullControl
        {
            get => MaskDenied.HasFlag(REGSAM.KEY_ALL_ACCESS);
            set => ToggleFlag(REGSAM.KEY_ALL_ACCESS, false, value);
        }

        public bool DenyRead
        {
            get => MaskDenied.HasFlag(REGSAM.KEY_READ);
            set => ToggleFlag(REGSAM.KEY_READ, false, value);
        }

        public bool DenySpecialPermissions // TODO
        {
            get => false;
            set => ToggleFlag(REGSAM.DELETE, false, value);
        }
        #endregion

        #region Methods
        private bool HasAllSpecialPermissions(bool allowedMask)
        {
            // TODO
            return false;
        }

        private void ToggleFlag(REGSAM target, bool allowedMask, bool value)
        {
            // TODO
        }
        #endregion
    }
}
