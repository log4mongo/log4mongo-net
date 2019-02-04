Framework "4.0"

Properties {
	$build_dir = Split-Path $psake.build_script_file
    $build_cfg = "Release"
}

Task default -Depends Test

Task Deps {
	Exec { & "nuget.exe" install NUnit.Runners -Version "2.6.0.12051" -ExcludeVersion -OutputDirectory "$build_dir\tools" }
}

Task Compile -Depends Deps {
    $version = Get-Content $build_dir\version.txt
    $copyright = "Gian Marco Gherardi " + (Get-Date).Year

	Exec { dotnet pack --configuration $build_cfg -p:Version=$version "-p:Copyright=$copyright" "$build_dir\src\Log4Mongo.sln" }
}

Task Test -Depends Compile {
    $TestDlls = Get-ChildItem "$build_dir\src\*\bin\$build_cfg\net45\publish\*.Tests.dll"
    Exec { & "$build_dir\tools\NUnit.Runners\tools\nunit-console.exe" /nologo /noresult /framework=4.0.30319 @TestDlls }
}

Task Publish -Depends Test {
    $version = Get-Content $build_dir\version.txt
    
    New-Item $build_dir\build -type directory

   	Exec { & "nuget.exe" pack "$build_dir\src\Log4Mongo\Log4Mongo.csproj" -Build -OutputDirectory $build_dir\build, -Symbols -Prop Configuration=$build_cfg }

    $version = $version -split "\."
    $version[2] =  [System.Int32]::Parse($version[2]) + 1
    $version -join "." | Out-File  $build_dir\version.txt -Encoding 'utf8'
}
