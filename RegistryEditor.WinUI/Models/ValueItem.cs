namespace RegistryValley.App.Models
{
    public class ValueItem : ObservableObject
    {
        public ValueItem()
        {
            Type = REG_VALUE_TYPE.REG_NONE;
            IsRenamable = true;
            DataIsEditable = true;
        }

        private string _name;
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private string _displayName;
        public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }

        private string _displayValue;
        public string DisplayValue { get => _displayValue; set => SetProperty(ref _displayValue, value); }

        private string _editableValue;
        public string EditableValue { get => _editableValue; set => SetProperty(ref _editableValue, value); }

        private uint _dataSize;
        public uint DataSize { get => _dataSize; set => SetProperty(ref _dataSize, value); }

        private REG_VALUE_TYPE _type;
        public REG_VALUE_TYPE Type { get => _type; set => SetProperty(ref _type, value); }

        private string _typeString;
        public string TypeString { get => _typeString; set => SetProperty(ref _typeString, value); }

        private bool _isRenamable;
        public bool IsRenamable { get => _isRenamable; set => SetProperty(ref _isRenamable, value); }

        private bool _dataIsEditable;
        public bool DataIsEditable { get => _dataIsEditable; set => SetProperty(ref _dataIsEditable, value); }
    }
}
