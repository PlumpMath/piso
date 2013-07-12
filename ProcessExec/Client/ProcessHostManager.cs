﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Threading;
using NLog;

namespace ProcessExec.Client
{
    public class ProcessHostManager : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly string processHostDirectory;
        private readonly string serviceName;
        private readonly string exeFileName;
        private readonly string containerDirectory;
        private readonly string processHostTargetDirectory;
        private readonly NetworkCredential credential;

        public ProcessHostManager(string processHostDirectory, string containerDirectory, string exeFileName, string serviceName, NetworkCredential credential)
        {
            if (!Directory.Exists(processHostDirectory))
            {
                throw new ArgumentException("Directory not found", "processHostDirectory");
            }
            this.processHostDirectory = processHostDirectory;

            if (string.IsNullOrWhiteSpace(exeFileName))
            {
                throw new ArgumentException("You must specify an executable file name.", "exeFileName");
            }
            this.exeFileName = exeFileName;

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException("Service name must be specified", "serviceName");
            }
            this.serviceName = serviceName;

            if (string.IsNullOrWhiteSpace(containerDirectory))
            {
                throw new ArgumentException("Container directory is not valid");
            }
            this.containerDirectory = containerDirectory;
            this.processHostTargetDirectory = Path.Combine(containerDirectory, "processhost");

            this.credential = credential;
        }

        private void CopyExecutableToHostTargetDirectory()
        {
            if (Directory.Exists(processHostTargetDirectory))
            {
                TryCleanServiceDirectory();
            }

            if (credential != null)
            {
                var acl = new FileSystemAccessRule(credential.UserName,
                                                   FileSystemRights.FullControl,
                                                   InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                   PropagationFlags.None,
                                                   AccessControlType.Allow);

                DirectorySecurity tempDirectorySecurity = new DirectoryInfo(containerDirectory).GetAccessControl();
                tempDirectorySecurity.AddAccessRule(acl);

                Directory.CreateDirectory(processHostTargetDirectory, tempDirectorySecurity);
            }
            else
            {
                Directory.CreateDirectory(processHostTargetDirectory);
            }

            log.Trace("Copying service files to: {0}", processHostTargetDirectory);
            foreach (var file in Directory.GetFiles(processHostDirectory))
            {
                File.Copy(file, file.Replace(processHostDirectory, processHostTargetDirectory));
            }
        }

        public void RunService()
        {
            try
            {
                CopyExecutableToHostTargetDirectory();
                InstallService();
                StartService();
            }
            catch (Exception ex)
            {
                log.ErrorException("Unable to run service", ex);
                throw;
            }
        }

        private void StartService()
        {
            log.Trace("Starting service {0}", serviceName);
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("Unable to stop service", ex);
            }
        }

        private void StopService()
        {
            log.Trace("Stopping service {0}", serviceName);
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("Unable to stop service", ex);
            }
        }

        private void InstallService()
        {
            if (ServiceExists())
            {
                log.Trace("Service '{0}' already installed, skipping.", serviceName);
                return;
            }

            log.Trace("Installing windows service {0}", serviceName);
            var binPath = Path.Combine(processHostTargetDirectory, exeFileName);

            if (credential != null)
            {
                RunServiceCommand(String.Format(@"create {0} binPath= ""{1}"" start= auto obj= {2} password= {3}",
                                                serviceName, binPath, credential.UserName, credential.Password));
            }
            else
            {
                RunServiceCommand(String.Format(@"create {0} binPath= ""{1}"" start= auto obj= LocalSystem", serviceName, binPath));
            }
        }

        private void UninstallService()
        {
            if (!ServiceExists())
            {
                log.Trace("Service '{0}' doesn't exist, skipping uninstall.", serviceName);
                return;
            }

            StopService();
            log.Trace("Removing service {0}", serviceName);
            TryRunServiceCommand(String.Format("delete {0}", serviceName));
        }

        private bool ServiceExists()
        {
            try
            {
                var result = ExecServiceCommand(String.Format("queryex {0}", serviceName), false);
                return result.ExitCode != 1060; // this sorta works except when a svc is marked for delete (reboot required perhaps?)
            }
            catch
            {
                return false;
            }
        }

        private void RunServiceCommand(string cmdArgs)
        {
            var result = ExecServiceCommand(cmdArgs, true);
            if (result.TimedOut || result.ExitCode != 0)
            {
                log.Error("Error or timeout running: sc.exe {0}", cmdArgs);
                throw new Exception("Error running service control command");
            }
        }

        private void TryRunServiceCommand(string cmdArgs)
        {
            try
            {
                var result = ExecServiceCommand(cmdArgs, true);
                if (result.TimedOut || result.ExitCode != 0)
                {
                    log.Error("Error or timeout running: sc.exe {0}", cmdArgs);
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(String.Format("Error running: sc.exe {0}", cmdArgs), ex);
            }
        }

        private ExecutableResult ExecServiceCommand(string cmdArgs, bool showOutput)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = "sc.exe";
                p.StartInfo.Arguments = cmdArgs;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                var result = p.StartWithRedirectedOutputIO(TimeSpan.FromMinutes(2), CancellationToken.None);

                if (showOutput && result.ExitCode != 0)
                {
                    log.Debug("Exit Code: {0}", result.ExitCode);

                    if (!String.IsNullOrWhiteSpace(result.StandardOut))
                    {
                        log.Debug("{0}{1}", Environment.NewLine, result.StandardOut);
                    }
                    if (!String.IsNullOrWhiteSpace(result.StandardError))
                    {
                        log.Debug("{0}{1}", Environment.NewLine, result.StandardError);
                    }
                    if (result.TimedOut)
                    {
                        log.Debug("Timed Out: {0}", result.TimedOut);
                    }
                }

                return result;
            }
        }

        private void TryCleanServiceDirectory()
        {
            try
            {
                if (Directory.Exists(processHostTargetDirectory))
                {
                    log.Trace("Cleaning up service directory {0}", processHostTargetDirectory);
                    Directory.Delete(processHostTargetDirectory, true);
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(String.Format("Unable to cleanup service directory {0}", processHostTargetDirectory), ex);
            }
        }

        public void Dispose()
        {
            try
            {
                UninstallService();
            }
            catch
            {
                
            }
            TryCleanServiceDirectory();
        }
    }
}
