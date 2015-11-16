mkdir release
mkdir GameData
mkdir GameData\ConnectedLivingSpace
mkdir GameData\ConnectedLivingSpace\assets
mkdir GameData\ConnectedLivingSpace\Plugins

copy assets\* GameData\ConnectedLivingSpace\assets
copy configs\* GameData\ConnectedLivingSpace\Configs

copy plugins\CLSInterfaces\bin\Release\CLSInterfaces.dll GameData\ConnectedLivingSpace\Plugins
copy plugins\ConnectedLivingSpace\bin\Release\ConnectedLivingSpace.dll GameData\ConnectedLivingSpace\Plugins

delete release\CLSv%1.zip

"c:\7zip\7za.exe" a -r release\CLSv%1.zip GameData\*
"c:\7zip\7za.exe" a -r release\CLSv%1.zip README.txt

rmdir /S /Q GameData

mkdir dev
copy plugins\CLSInterfaces\bin\Release\CLSInterfaces.dll dev

copy plugins\CLSInterfaces\CLSClient.cs dev

"c:\7zip\7za.exe" a -r dev\CLSDevPackv%1.zip dev\*
"c:\7zip\7za.exe" a -r release\CLSv%1.zip dev\CLSDevPackv%1.zip

rmdir /S /Q dev

