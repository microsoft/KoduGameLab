// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace BokuSetupTool
{
    public static class TFS
    {
        public static bool EditFile(string filename)
        {
            try
            {
                var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(filename);
                var uri = new Uri(workspaceInfo.ServerUri.Scheme + "://" + workspaceInfo.ServerUri.Host);
                var tfsConfig = TfsConfigurationServerFactory.GetConfigurationServer(uri);
                tfsConfig.EnsureAuthenticated();
                using (var server = new TfsTeamProjectCollection(workspaceInfo.ServerUri, tfsConfig.Credentials))
                {
                    var workspace = workspaceInfo.GetWorkspace(server);
                    int nResult = workspace.PendEdit(filename);
                    return (nResult == 1);
                }
            }
            catch
            {
                throw (new Exception("Error checking out " + filename));
            }
        }
    }
}
