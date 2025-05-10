using System;
using System.IO;
using Abp.Reflection.Extensions;

namespace ABPNET
{
    /// <summary>
    /// Centralized point for application version value.
    /// IMPORTANT, Version are empty when building the project on linux
    /// ref: https://github.com/dotnet/core/issues/3916
    /// </summary>
    public class AppVersionHelper
    {
        /// <summary>
        /// Gets current version of the application.
        /// It's also shown in the main view.
        /// </summary>
        private Lazy<string> LzyVersion = new Lazy<string>(() =>
        {
            // Edit version: right click, ABPNET.Core project  > Properties > Package > set Assembly file version
            System.Reflection.Assembly assembly = typeof(AppVersionHelper).GetAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            if (fvi != null && fvi.FileVersion.Length > 0)
            {
                return fvi.FileVersion;
            }
            string appVersion = assembly?.GetName().Version.ToString();
            return appVersion;
        });

        public string Version => LzyVersion.Value;

        /// <summary>
        /// Gets release (last build) date of the application.
        /// It's shown in the web page.
        /// </summary>
        private Lazy<DateTime> LzyReleaseDate = new Lazy<DateTime>(() =>
        {
            return new FileInfo(typeof(AppVersionHelper).GetAssembly().Location).LastWriteTime;
        });

        public DateTime ReleaseDate => LzyReleaseDate.Value;
    }
}



