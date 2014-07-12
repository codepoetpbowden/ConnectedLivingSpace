Connected Living Space v1.0.6.0
---------------------------

To install copy the GameData folder to your KSP folder. Be aware that a release of the Toolbar mod is included. Module Manager is required to load the configuration.

If you are integrating a mod with CLS then the contents of the dev directory has all you will need - an assebly that defines the interfaces, and a suggested snippet of code for testing for and accessing CLS.

Thread for discussion: http://forum.kerbalspaceprogram.com/threads/70161
Please log bugs in github if possible: https://github.com/codepoetpbowden/ConnectedLivingSpace
If you can use github then report in the forum.

License:
--------

ConnectedLivingSpace is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

changelog:
----------

release v1.0.6.0
* Added support for the Novapunch parts pack
* Fixed the bug of the hatch status not being saved and loaded properly.
* Cleaned up the error log a bit
* Made hatches openable and closable id two docking ports are attached rather than being docked.
* Made all hatches open in the VAB so the extend of spaces can be checked at designed time.

release v1.0.5.0
* Added interfaces to be used by other mods that use CLS functionality
* Added Support for Extra Planetary launchpads 
* Added support for Station Parts Expansion
* Fixed config bugs where impassablenodes had been used without setting the passable option
* Changed standard 1.25m dockingports to be passabel when surface mounted
* Some changes to the UI

release v1.0.4.1 
* Warning no longer logged for EVAs
* Added instruction for writing config files.
* Added support for surface attached parts
* Fixed bug of space names being lost. 
* Added CrewCabin to stock config 
* Added config for KSOS part pack. 
* Moved implementation of hatches into the ModuleDockingNodeHatch
* Changed the handling of docked connections to support docking ports that do not use an attach node, and also parts with more than one docking port.
* Fixed spelling mistake in the name of Sun Dum Heavy Industries
* Added support for Large Structural/Station components part pack
* Added support for Home Grown Rockets part pack. 
* Added support for Universal Storage part pack.
* Added support for B9 aerospace parts pack. 
* Added support for KSPX parts pack.

release v1.0.3.0
* Automatically adds a CLSModule to parts with crewcapacity>0
* Added choice of stock configs
* Added config for ksomos pack
* Changed the handling of part / space highlighting

release v1.0.2.0
* Rebuilt against .net 3.5 runtime

release v1.0.1.0
* Added config for FusTek parts
* Added config for Porkworks parts
* Added config for DumSum Heavy Industries parts
* Added config for ASET stackable inline lights
* Added the concept of hatches that can be opened or close in docking ports
* Tidied uo the GUI
* Added info to the editor about whether a part if naviagable etc
* Added config for KWRocketry docking rings.


release v1.0.0.0
*Added Instance and Vessel to the Addon class
*Fixed bugs

pre-release v0.3
* Added the CLSKerbal class
* Added a pretty button for the toolbar.