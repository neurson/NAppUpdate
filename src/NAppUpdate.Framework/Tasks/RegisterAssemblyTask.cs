using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NAppUpdate.Framework.Common;
using NAppUpdate.Framework.Sources;

namespace NAppUpdate.Framework.Tasks
{
    [Serializable]
    [UpdateTaskAlias("RegisterAssembly")]
    public class RegisterAssemblyTask : UpdateTaskBase
    {
        private string _regasmPath;
        private string _appPath;

        [NauField("localPath", "The local path of the file to update", true)]
        public string LocalPath { get; set; }

        [NauField("Force32BitRegistration", "Force 32 bit registration of the assembly on 64 bit system.", false)]
        public bool Force32BitRegistration { get; set; }

        public RegisterAssemblyTask()
        {
        }

        public override void Prepare(IUpdateSource source)
        {
            var runtimePath = RuntimeEnvironment.GetRuntimeDirectory();

            if (Force32BitRegistration)
            {
                runtimePath = runtimePath.Replace("Framework64", "Framework");
            }

            _regasmPath = Path.Combine(runtimePath,"regasm.exe");

            _appPath = Path.GetDirectoryName(UpdateManager.Instance.ApplicationPath);

            Description = string.Format("Registing assembly: {0}", LocalPath);
        }

        public override TaskExecutionStatus Execute(bool coldRun)
        {
            if (!coldRun)
            {
                return TaskExecutionStatus.RequiresPrivilegedAppRestart;
            }

            try
            {
                var assemblyPath = Path.Combine(_appPath, LocalPath);
                var arguments = string.Format("/codebase \"{0}\"", assemblyPath);

                var info = new ProcessStartInfo(_regasmPath, arguments)
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };

                var p = Process.Start(info);

                p.WaitForExit(30000);

                if (p.ExitCode != 0)
                {
                    ExecutionStatus = TaskExecutionStatus.Failed;

                    UpdateManager.Instance.Logger.Log(Logger.SeverityLevel.Error, 
                        string.Format("Could not register the assembly: {0}.", LocalPath));

                    throw new UpdateProcessFailedException("Could not register the assembly");
                }

                UpdateManager.Instance.Logger.Log(
                    string.Format("Assembly registred successfuly: {0}.", LocalPath));

                return ExecutionStatus = TaskExecutionStatus.Successful;
            }

            catch (Exception ex)
            {
                ExecutionStatus = TaskExecutionStatus.Failed;

                UpdateManager.Instance.Logger.Log(ex,
                    string.Format("Could not register the assembly: {0}.", LocalPath));

                throw new UpdateProcessFailedException("Could not register the assembly", ex);
            }
        }

        public override bool Rollback()
        {
            var assemblyPath = Path.Combine(_appPath, LocalPath);
            var arguments = string.Format("/unregister \"{0}\"", assemblyPath);

            Process.Start(_regasmPath, arguments);

            UpdateManager.Instance.Logger.Log(
                   string.Format("Rollback - assembly unregistred: {0}.", LocalPath));

            return true;
        }
    }
}