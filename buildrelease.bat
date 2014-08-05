mkdir release
mkdir GameData
mkdir GameData\ConnectedLivingSpace
mkdir GameData\ConnectedLivingSpace\assets
mkdir GameData\ConnectedLivingSpace\Plugins

copy assets\* GameData\ConnectedLivingSpace\assets
copy configs\* GameData\ConnectedLivingSpace\Plugins

copy plugins\CLSInterfaces\bin\Release\CLSInterfaces.dll GameData\ConnectedLivingSpace\Plugins
copy plugins\ConnectedLivingSpace\bin\Release\ConnectedLivingSpace.dll GameData\ConnectedLivingSpace\Plugins

delete release\CLSv%1.zip

"c:\program files (x86)\7zip\7za.exe" a -r release\CLSv%1.zip GameData\*

rmdir /S /Q GameData

mkdir dev
copy plugins\CLSInterfaces\bin\Release\CLSInterfaces.dll dev

copy plugins\CLSInterfaces\CLSClient.cs dev

"c:\program files (x86)\7zip\7za.exe" a -r dev\CLSDevPackv%1.zip dev\*
"c:\program files (x86)\7zip\7za.exe" a -r release\CLSv%1.zip dev\CLSDevPackv%1.zip

rmdir /S /Q dev

