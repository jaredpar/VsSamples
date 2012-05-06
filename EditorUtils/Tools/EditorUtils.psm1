
# Locate the source.extension.vsixmanifest file in the current project.  Typically
# this will be in the root folder but it's possible for it to be in a sub-folder
# as well 
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

Export-ModuleMember Get-ManifestFilePath
