using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Hardware.Fingerprints;
using Android.OS;
using Java.Lang;
using SMS.Fingerprint.Abstractions;

namespace SMS.Fingerprint
{
    internal class FingerprintImplementation : IFingerprint
    {
        public bool IsAvailable => CheckAvailability();

        public Task<FingerprintAuthenticationResult> AuthenticateAsync(string reason)
        {
            return AuthenticateAsync(reason, new CancellationToken());
        }

        public async Task<FingerprintAuthenticationResult> AuthenticateAsync(string reason, CancellationToken cancellationToken)
        {
            if (!IsAvailable)
            {
                return new FingerprintAuthenticationResult { Status = FingerprintAuthenticationResultStatus.NotAvailable };
            }

            if (Fingerprint.DialogEnabled)
            {
                var fragment = Fingerprint.CreateDialogFragment();
                return await fragment.ShowAsync(reason, cancellationToken);
            }

            return await AuthenticateNoDialogAsync(cancellationToken);
        }

        internal static async Task<FingerprintAuthenticationResult> AuthenticateNoDialogAsync(CancellationToken cancellationToken)
        {
            var cancellationSignal = new CancellationSignal();
            var callback = new FingerprintAuthenticationCallback();
            cancellationToken.Register(() => cancellationSignal.Cancel());

            Fingerprint.GetService().Authenticate(null, cancellationSignal, FingerprintAuthenticationFlags.None, callback, null);

            return await callback.GetTask();
        }

        private bool CheckAvailability()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                return false;

            var context = Application.Context;
            if (context.CheckCallingOrSelfPermission(Manifest.Permission.UseFingerprint) != Permission.Granted)
                return false;

            var fpService = (FingerprintManager)context.GetSystemService(Class.FromType(typeof(FingerprintManager)));
            if (!fpService.IsHardwareDetected)
                return false;

            if (!fpService.HasEnrolledFingerprints)
                return false;

            return true;
        }
    }
}
