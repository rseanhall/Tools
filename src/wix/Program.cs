// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Tools.Core;

    /// <summary>
    /// Wix Toolset Command-Line Interface.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// The main entry point for wix command-line interface.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            var serviceProvider = new WixToolsetServiceProvider();

            var listener = new ConsoleMessageListener("WIX", "wix.exe");

            var program = new Program();
            return program.Run(serviceProvider, listener, args);
        }

        /// <summary>
        /// Executes the wix command-line interface.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use throughout this execution.</param>
        /// <param name="args">Command-line arguments to execute.</param>
        /// <returns>Returns the application error code.</returns>
        public int Run(IServiceProvider serviceProvider, IMessageListener listener, string[] args)
        {
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(args);

            var commandLine = serviceProvider.GetService<ICommandLineParser>();
            commandLine.ExtensionManager = CreateExtensionManagerWithStandardBackends(serviceProvider, messaging, arguments.Extensions);
            commandLine.Arguments = arguments;
            var command = commandLine.ParseStandardCommandLine();
            return command?.Execute() ?? 1;
        }

        private static IExtensionManager CreateExtensionManagerWithStandardBackends(IServiceProvider serviceProvider, IMessaging messaging, string[] extensions)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            foreach (var extension in extensions)
            {
                try
                {
                    extensionManager.Load(extension);
                }
                catch (ReflectionTypeLoadException e)
                {
                    messaging.Write(ErrorMessages.InvalidExtension(extension, String.Join(Environment.NewLine, e.LoaderExceptions.Select(le => le.ToString()))));
                }
            }

            return extensionManager;
        }
    }
}
