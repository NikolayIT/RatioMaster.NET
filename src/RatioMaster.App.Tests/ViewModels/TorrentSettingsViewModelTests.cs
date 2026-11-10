namespace RatioMaster.App.Tests.ViewModels
{
    using RatioMaster.App.ViewModels;
    using RatioMaster.Core.Clients;
    using Xunit;

    public sealed class TorrentSettingsViewModelTests
    {
        [Fact]
        public void ClearingABaseSpeedPreventsSavingUntilCorrected()
        {
            var editor = CreateEditor();
            Assert.True(editor.IsSpeedSettingsValid);

            editor.UploadKb = null;
            Assert.False(editor.IsSpeedSettingsValid);
            Assert.NotNull(editor.SpeedValidationMessage);

            editor.UploadKb = 0m;
            Assert.True(editor.IsSpeedSettingsValid);
            Assert.Null(editor.SpeedValidationMessage);
        }

        [Fact]
        public void ClearingAnEnabledRandomRangePreventsSaving()
        {
            var editor = CreateEditor();
            editor.NextUpdateRandomUpload = true;
            editor.NextUpdateUploadMinKb = null;

            Assert.False(editor.IsSpeedSettingsValid);

            editor.NextUpdateUploadMinKb = 10m;
            Assert.True(editor.IsSpeedSettingsValid);
        }

        [Fact]
        public void ReversedRangeIsRejectedButIgnoredWhenDisabled()
        {
            var editor = CreateEditor();
            editor.NextUpdateRandomDownload = true;
            editor.NextUpdateDownloadMinKb = 70m;
            editor.NextUpdateDownloadMaxKb = 60m;

            Assert.False(editor.IsSpeedSettingsValid);
            Assert.Contains("minimum", editor.SpeedValidationMessage!);

            editor.NextUpdateRandomDownload = false;
            Assert.True(editor.IsSpeedSettingsValid);
        }

        [Fact]
        public void NonIntegralRandomRangeIsRejected()
        {
            var editor = CreateEditor();
            editor.UploadRandomMinKb = 1.5m;

            Assert.False(editor.IsSpeedSettingsValid);

            editor.UploadRandomMinKb = 2m;
            Assert.True(editor.IsSpeedSettingsValid);
        }

        [Fact]
        public void ChangedValuesAreSerializedWithoutLosingPrecision()
        {
            var editor = CreateEditor();
            editor.UploadKb = 12.5m;
            editor.DownloadKb = 3.25m;
            editor.NextUpdateRandomUpload = true;
            editor.NextUpdateUploadMinKb = 15m;
            editor.NextUpdateUploadMaxKb = 30m;

            var settings = editor.ToSettings();

            Assert.Equal(12800, settings.UploadRateBytes);
            Assert.Equal(3328, settings.DownloadRateBytes);
            Assert.Equal(15, settings.NextUpdateUploadMinKb);
            Assert.Equal(30, settings.NextUpdateUploadMaxKb);
        }

        [Fact]
        public void ModelDoesNotOverflowWhenADisabledRangeContainsAnOutOfRangeValue()
        {
            var editor = CreateEditor();
            editor.UploadRandomEnabled = false;
            editor.UploadRandomMaxKb = decimal.MaxValue;

            Assert.True(editor.IsSpeedSettingsValid);
            Assert.Equal(int.MaxValue, editor.ToSettings().UploadRandomMaxKb);
        }

        [Fact]
        public void RangeValidationRaisesPropertyChangeNotifications()
        {
            var editor = CreateEditor();
            var raised = 0;
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(TorrentSettingsViewModel.IsSpeedSettingsValid))
                {
                    raised++;
                }
            };

            editor.UploadRandomMaxKb = 0m;
            Assert.False(editor.IsSpeedSettingsValid);

            editor.UploadRandomMaxKb = 10m;
            Assert.True(editor.IsSpeedSettingsValid);
            Assert.Equal(2, raised);
        }

        private static TorrentSettingsViewModel CreateEditor() => new(ClientProfileCatalog.Load());
    }
}
