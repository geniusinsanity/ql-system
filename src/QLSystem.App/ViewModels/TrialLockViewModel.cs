using System;
using System.Windows.Input;
using QLSystem.App.Helpers;
using QLSystem.Core.Models;
using QLSystem.Licensing;

namespace QLSystem.App.ViewModels
{
    /// <summary>
    /// إدارة شاشة قفل انتهاء الـ 7 أيام التجريبية والتفعيل بالرمز
    /// </summary>
    public class TrialLockViewModel : ObservableObject
    {
        private readonly TrialManager _trialManager;
        private readonly Action _onSuccessfullyActivated;

        private string _machineId = string.Empty;
        private string _activationKeyInput = string.Empty;
        private string _errorMessage = string.Empty;
        private string _statusMessage = string.Empty;
        private string _whatsappContact = "0550 12 34 56";

        public string MachineId
        {
            get => _machineId;
            set => SetProperty(ref _machineId, value);
        }

        public string ActivationKeyInput
        {
            get => _activationKeyInput;
            set => SetProperty(ref _activationKeyInput, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string WhatsappContact
        {
            get => _whatsappContact;
            set => SetProperty(ref _whatsappContact, value);
        }

        public ICommand ActivateCommand { get; }

        public TrialLockViewModel(TrialManager trialManager, Action onSuccessfullyActivated)
        {
            _trialManager = trialManager;
            _onSuccessfullyActivated = onSuccessfullyActivated;

            MachineId = HardwareFingerprint.GetMachineId();
            StatusMessage = "انتهت فترة الـ 7 أيام التجريبية المجانية. جميع بياناتك ومبيعاتك محفوظة بأمان. اتصل بالمطور لتفعيل نسختك الدائمة.";

            ActivateCommand = new RelayCommand(ExecuteActivate);
        }

        private void ExecuteActivate()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ActivationKeyInput))
            {
                ErrorMessage = "يرجى إدخال مفتاح التفعيل أولاً.";
                return;
            }

            var success = _trialManager.Activate(ActivationKeyInput.Trim());
            if (success)
            {
                _onSuccessfullyActivated?.Invoke();
            }
            else
            {
                ErrorMessage = "مفتاح التفعيل غير صحيح لهذا الجهاز. يرجى التأكد وإعادة المحاولة.";
            }
        }
    }
}
