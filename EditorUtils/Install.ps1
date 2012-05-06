param($installPath, $toolsPath, $package, $project)


# Locate the source.extension.vsixmanifest file in the current project.  Typically
# this will be in the root folder but it's possible for it to be in a sub-folder
# as well 
function Get-ManifestFilePath() {
    param ($path = $(throw "Need a base path"),
           $current = $script:project)

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
            $manifest = Get-ManifestFilePath $item $item.Name
            if ($manifest -ne $null) {
                return $manifest
            }
        }
    }

    return $null
}


# Need to add the EditorUtil.dll into the source.extension.vsixmanifest file.  First
# step is to find the file itself.  It may or may not be present in the project we
# are installing into.  It's perfectly legal for a project to not have a manifest
# file if it's just a utility project itself.  Only the actual VSIX project will
# have one.
Function Add-MefReference() {
    $basePath = Split-Path -parent $script:project.FullName
    $manifestFilePath = Get-ManifestFilePath $basePath

    # No manifest file isn't a problem
    if ($manifestFilePath -eq $null) {
        Write-Host "No source.extension.vsixmanifest found"
        return
    }

    Write-Host "Found manifest: " + $manifestFilePath
    $x = [xml](gc $manifestFilePath)
    $found = $false
    foreach ($item in $x.Vsix.Content.MefComponent) {
        if ($item -eq "EditorUtils.dll") {
            $found = $true
        }
    }

    if (-not $found) {
        Write-Host "Adding MefComponent reference to EditorUtils.dll"
        $node = $x.Vsix.Content.ChildNodes.Item(0).Clone()
        $node.set_InnerText("EditorUtils.dll")
        $x.Vsix.Content.AppendChild($node)
        $x.Save($manifestFilePath)
    }
}

Add-MefReference

