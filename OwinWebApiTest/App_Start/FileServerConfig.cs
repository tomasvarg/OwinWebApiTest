using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace OwinWebApiTest
{
    /**
     * Configuration of the static file serving options
     */
    public static class FileServerConfig
    {
        public static FileServerOptions Create(PathString pathString, string dir)
        {
            string appRoot = AppDomain.CurrentDomain.BaseDirectory;
            var fileSystem = new PhysicalFileSystem(Path.Combine(appRoot, dir));

            var options = new FileServerOptions
            {
                RequestPath = pathString,
                EnableDefaultFiles = true,
                FileSystem = fileSystem
            };
            options.StaticFileOptions.FileSystem = fileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;

            return options;
        }
    }
}