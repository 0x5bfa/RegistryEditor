// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;

namespace RegistryEditor.WinUI.Converters
{
    public partial class ValueTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = false;

            if (parameter is string strParam)
            {
                invert = strParam.ToLower() == "invert" ? true : false;
            }

            if (value is REG_VALUE_TYPE type)
            {
                switch (type)
                {
                    case REG_VALUE_TYPE.REG_SZ:
                    case REG_VALUE_TYPE.REG_EXPAND_SZ:
                    case REG_VALUE_TYPE.REG_MULTI_SZ:
                        {
                            return !invert;
                        }
                    default:
                    case REG_VALUE_TYPE.REG_BINARY:
                    case REG_VALUE_TYPE.REG_DWORD:
                    case REG_VALUE_TYPE.REG_QWORD:
                        {
                            return invert;
                        }
                }
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
