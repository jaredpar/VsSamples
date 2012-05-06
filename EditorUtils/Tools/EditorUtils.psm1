
###############################################################################
#
# Locate the source.extension.vsixmanifest file in the current project.  Typically
# this will be in the root folder but it's possible for it to be in a sub-folder
# as well 
#
###############################################################################
function Get-ManifestFilePathCore() {
    param ($path = $(throw "Need a base path"),
           $current = $(throw "Need a search point"))

    # First look at the root files
    foreach ($item in $current.ProjectItems) {
        if ($item.Name -eq "source.extension.vsixmanifest") {
            return Join-Path $path $item.Name;
        }
    }

    # Now dig into any child project items
    foreach ($item in $current.ProjectItems) {
        $kind = $item.Kind;
        if ($kind -eq "{6bb5f8ef-4483-11d3-8bcf-00c04f8ec28c}") {
            $manifest = Get-ManifestFilePathCore (Join-Path $path $item.name) $item
            if ($manifest -ne $null) {
                return $manifest
            }
        }
    }

    return $null
}

function Get-ManifestFilePath() {
    param ($project = $(throw "Need a project to get the manifest file in"))

    $basePath = Split-Path -parent $project.FullName
    return Get-ManifestFilePathCore $basePath $project
}

# Assemblies valid in any version.  These are all versioned assemblies and 
# must either be BCL types or types which are explicitly versioned in the 
# devenv.exe.config file
#
# Note: The Shell.XXX assemblies are all COM assemblies that aren't versioned
# but their COM so they don't change either.  Fine to reference from any 
# version after the one they were defined in
$listAll = @(
    "envdte=8.0.0.0",
    "envdte80=8.0.0.0",
    "envdte90=9.0.0.0",
    "envdte100=10.0.0.0",
    "FSharp.Core=4.0.0.0",
    "Microsoft.VisualStudio.CoreUtility=10.0.0.0",
    "Microsoft.VisualStudio.Editor=10.0.0.0",
    "Microsoft.VisualStudio.OLE.Interop=7.1.40304.0"
    "Microsoft.VisualStudio.Language.Intellisense=10.0.0.0",
    "Microsoft.VisualStudio.Language.StandardClassification=10.0.0.0",
    "Microsoft.VisualStudio.Shell=2.0.0.0",
    "Microsoft.VisualStudio.Shell.10.0=10.0.0.0",
    "Microsoft.VisualStudio.Shell.Immutable.10.0=10.0.0.0",
    "Microsoft.VisualStudio.Shell.Interop=7.1.40304.0",
    "Microsoft.VisualStudio.Shell.Interop.8.0=8.0.0.0",
    "Microsoft.VisualStudio.Shell.Interop.9.0=9.0.0.0",
    "Microsoft.VisualStudio.Shell.Interop.10.0=10.0.0.0",
    "Microsoft.VisualStudio.Text.Logic=10.0.0.0",
    "Microsoft.VisualStudio.Text.Data=10.0.0.0",
    "Microsoft.VisualStudio.Text.UI=10.0.0.0",
    "Microsoft.VisualStudio.Text.UI.Wpf=10.0.0.0",
    "Microsoft.VisualStudio.TextManager.Interop=7.1.40304.0",
    "Microsoft.VisualStudio.TextManager.Interop.8.0=8.0.0.0",
    "Microsoft.VisualStudio.Platform.VSEditor.Interop=10.0.0.0",
    "mscorlib=",
    "PresentationCore="
    "PresentationFramework="
    "System=",
    "System.ComponentModel.Composition=",
    "System.Core=",
    "System.Data=",
    "System.Data.DataSetExtensions=",
    "System.Drawing=",
    "System.Xml=",
    "System.Xaml=",
    "WindowsBase=",
    "Microsoft.CSharp=",
    "EditorUtils="
)

# Types specific to Dev10
$list10 = @(
    "Microsoft.VisualStudio.Platform.WindowManagement=10.0.0.0",
    "Microsoft.VisualStudio.Shell.ViewManager=10.0.0.0"
)
$list10 = $list10 + $listAll

# Types specific to Dev11
$list11 = @(
    "Microsoft.VisualStudio.Platform.WindowManagement=11.0.0.0",
    "Microsoft.VisualStudio.Shell.11.0=11.0.0.0",
    "Microsoft.VisualStudio.Shell.ViewManager=11.0.0.0"
)
$list11 = $list11 + $listAll

# Test assemblies are allowed to reference a couple of DLL's needed
# to host the editor but aren't legal / recomended for normal
# code.  These DLL's don't version and should be accessed instead
# through other interfaces
$listTest = @(
    "Microsoft.VisualStudio.Platform.VSEditor=10.0.0.0",
    "Moq=4.0.10827.0",
    "nunit.framework=2.6.0.12051"
)
$listTest = $listTest + $listAll + $list10

function Test-Reference() {
    param ([string]$dll = $(throw "Need a dll name to check"),
           $list = $(throw "Need a target list"))

    foreach ($validDll in $list) {
        if ($validDll -eq $dll) {
            return $true;
        }

        if ($validDll.StartsWith($dll)) {
            $all = $validDll.Split('=')
            Write-Host "`tWarning: Reference to $($all[0]) is lacking a specific version, expected $($all[1])"
            return $true
        }
    }

    return $false;
}

function Test-Include() {
    param ([string]$project = $(throw "Need a project string"),
           [string]$reference = $(throw "Need a reference string"),
           $list = $(throw "Need a target list"))
    if (-not ($reference -match "^[^,]*")) {
        Write-Error "Error! Invalid reference: $reference"
    }

    $dll = $matches[0];
    $version = "";
    if ($reference -match "Version=([\d.]+)") {
        $version = $matches[1]
    }

    $dllWithVersion = "{0}={1}" -f $dll, $version;
    if (-not (Test-Reference $dllWithVersion $list)) {
        Write-Host "Invalid reference $dll in $project"
    }
}

###############################################################################
#
# This script is used to validate projects don't inadvertently reference a 
# DLL which isn't legal for the set of Visual Studio versions the project
# must work in.
#
# Most DLLs used by Visual Studio extensions have appropriate redirects in 
# devenv.exe.config and hence will work across Visual Studio versions. There is
# a set though which doesn't and hence must not be used in projects which are
# intended to work with all Visual Studio versions.  This script will scrictly
# evaluate references in a project for compliance.  
#
# A given project can specify which version of Visual Studio it wants to work
# with by adding a VisualStudioTarget element into the XML file.  It can have
# the following values
#
#   <VisualStudioTarget>all</VisualStudioTarget>
#   <VisualStudioTarget>test</VisualStudioTarget>
#   <VisualStudioTarget>Dev10</VisualStudioTarget>
#   <VisualStudioTarget>Dev11</VisualStudioTarget>
#
###############################################################################
function Test-ProjectFile() {
    param ($project = $(throw "Need a DTE Project object"))

    $ns = @{
        msb = "http://schemas.microsoft.com/developer/msbuild/2003"
    }

    $path = $project.FullName
    $name = split-path -leaf $path
    write-host "Checking $name"

    $x = [xml](gc $path) 
    $result = $x | Select-Xml -xpath "//msb:VisualStudioTarget" -namespace $ns
    if ($result -eq $null) {
        Write-Host "`tWarning: No VisualStudioTarget Element found, assuming all version compliance"
        $version = "Dev10"
    } else {
        $version = $result.Node.get_InnerText()
    }
        
    switch ($version) {
        "all" { $list = $listAll }
        "test" { $list = $listTest }
        "Dev10" { $list = $list10 }
        "Dev11" { $list = $list11 }
        default { Write-Error "Unrecognized version: $version" }
    }

    foreach ($line in gc $path) {
        if ($line -match "<Reference Include=`"([^`"]*)`"") {
            Test-Include $name $matches[1] $list
            $count += 1
        }
    }

    if ($count -eq 0) {
        write-error "Couldn't find any references: $path"
    }
}

function Test-AllProjectFile() {
    foreach ($project in $dte.Solution.Projects) {
        Test-ProjectFile $project
    }
}

Export-ModuleMember Get-ManifestFilePath
Export-ModuleMember Test-ProjectFile
Export-ModuleMember Test-AllProjectFile
