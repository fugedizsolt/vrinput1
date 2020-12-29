SET PATH_SAJAT=D:\tmp\GitSajat\unity-mapexplorer
SET RAMDISK_PATH=R:\unity-mapexplorer

mkdir %RAMDISK_PATH%

mklink /D %RAMDISK_PATH%\Assets           %PATH_SAJAT%\Assets
mklink /D %RAMDISK_PATH%\Packages         %PATH_SAJAT%\Packages
mklink /D %RAMDISK_PATH%\ProjectSettings  %PATH_SAJAT%\ProjectSettings
