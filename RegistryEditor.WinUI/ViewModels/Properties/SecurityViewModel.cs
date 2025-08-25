using RegistryValley.App.Models;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace RegistryValley.App.ViewModels.Properties
{
    public class SecurityViewModel : ObservableObject
    {
        public SecurityViewModel()
        {
            _principals = new();
            Principals = new(_principals);
        }

        #region Fields and Properties
        private KeyItem _keyItem;
        public KeyItem KeyItem { get => _keyItem; set => SetProperty(ref _keyItem, value); }

        private readonly ObservableCollection<PermissionPrincipalItem> _principals;
        public ReadOnlyObservableCollection<PermissionPrincipalItem> Principals { get; }

        private PermissionPrincipalItem _selectedPrincipal;
        public PermissionPrincipalItem SelectedPrincipal { get => _selectedPrincipal; set => SetProperty(ref _selectedPrincipal, value); }

        private PermissionPrincipalItem _securityDescriptorOwner;
        public PermissionPrincipalItem SecurityDescriptorOwner { get => _securityDescriptorOwner; set => SetProperty(ref _securityDescriptorOwner, value); }

        private bool _hasDacl;
        public bool HasDacl { get => _hasDacl; set => SetProperty(ref _hasDacl, value); }
        #endregion

        public void GetKeyAccessControlList()
        {
            try
            {
                Win32Error result;
                bool bResult;
                var pSD = new SafePSECURITY_DESCRIPTOR(1024);
                var cbSD = (uint)pSD.Size;

                // Win32API
                result = RVRegOpenKey(KeyItem.RootHive, KeyItem.Path, REGSAM.KEY_READ, out var phKey);
                if (result.Failed)
                {
                    Kernel32.SetLastError((uint)result);
                    return;
                }

                // Win32API
                result = RegGetKeySecurity(phKey, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, pSD, ref cbSD);
                if (result.Failed)
                {
                    Kernel32.SetLastError((uint)result);
                    return;
                }

                // Win32API
                bResult = GetSecurityDescriptorDacl(pSD, out var lpbDaclPresent, out var pDacl, out bool lpbDaclDefaulted);
                result = Kernel32.GetLastError();
                if (result.Failed)
                {
                    Kernel32.SetLastError((uint)result);
                    return;
                }

                if (!lpbDaclPresent)
                {
                    HasDacl = false;
                    return;
                }
                else
                {
                    // Win32API
                    bResult = GetAclInformation(pDacl, out ACL_SIZE_INFORMATION asi, (uint)Marshal.SizeOf(typeof(ACL_SIZE_INFORMATION)), ACL_INFORMATION_CLASS.AclSizeInformation);
                    result = Kernel32.GetLastError();
                    if (result.Failed)
                    {
                        Kernel32.SetLastError((uint)result);
                        return;
                    }

                    HasDacl = true;

                    GetKeyAccessControlEntries(pDacl, asi.AceCount);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void GetKeyAccessControlEntries(PACL pDacl, uint nAceCount)
        {
            Win32Error result;
            bool bResult;

            SafePSID lpSid;
            AceType aceType;
            ACE_HEADER aceHeader;
            bool aceIsObjectAce;
            uint accessMask;

            try
            {
                for (var index = 0U; index < nAceCount; index++)
                {
                    // Win32API
                    bResult = GetAce(pDacl, index, out var pAce);
                    result = Kernel32.GetLastError();

                    lpSid = pAce.GetSid();
                    aceType = pAce.GetAceType();
                    aceHeader = pAce.GetHeader();
                    aceIsObjectAce = pAce.IsObjectAce();
                    accessMask = pAce.GetMask();

                    bool isInherited = aceHeader.AceFlags.HasFlag(AceFlags.Inherited);

                    var cchName = 2048;
                    var cchReferencedDomainName = 2048;
                    var lpName = new StringBuilder(cchName, cchName);
                    var lpReferencedDomainName = new StringBuilder(cchReferencedDomainName, cchReferencedDomainName);

                    bResult = LookupAccountSid(null, lpSid, lpName, ref cchName, lpReferencedDomainName, ref cchReferencedDomainName, out var snu);
                    result = Kernel32.GetLastError();

                    // Uknown SID type
                    if (result.Failed && result == Win32Error.ERROR_NONE_MAPPED)
                    {
                        // Reset
                        lpName.Clear();
                        lpReferencedDomainName.Clear();
                    }

                    // Referenced domain name will be used as computer log on name
                    if (lpReferencedDomainName.ToString() == "BUILTIN")
                    {
                        lpReferencedDomainName.Clear();
                        lpReferencedDomainName = new(256, 256);
                        uint size = (uint)lpReferencedDomainName.Capacity;
                        bResult = Kernel32.GetComputerName(lpReferencedDomainName, ref size);
                    }
                    else
                    {
                        lpReferencedDomainName.Clear();
                    }

                    var principal = new PermissionPrincipalItem()
                    {
                        SidType = snu,
                        Sid = pAce.GetSid().ToString(),
                        Name = lpName.ToString(),
                        Domain = lpReferencedDomainName.ToString().ToLower(),
                    };

                    if (aceType == AceType.AccessAllowed)
                    {
                        principal.AccessRuleMerged = new()
                        {
                            MaskAllowed = (REGSAM)accessMask,

                            // REMOVE: This is temporary code to disable to change access control
                            //IsInheritedAllowedMask = isInherited,
                            IsInheritedAllowedMask = true,
                            IsInheritedDeniedMask = true,
                        };
                    }
                    else if (aceType == AceType.AccessDenied)
                    {
                        principal.AccessRuleMerged = new()
                        {
                            MaskDenied = (REGSAM)accessMask,

                            // REMOVE: This is temporary code to disable to change access control
                            //IsInheritedDeniedMask = isInherited,
                            IsInheritedAllowedMask = true,
                            IsInheritedDeniedMask = true,
                        };
                    }

                    _principals.Add(principal);

                    lpName?.Clear();
                    lpReferencedDomainName?.Clear();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
