Connected Living Space v1.1.4.0
---------------------------

To install copy the GameData folder to your KSP folder. Module Manager is required to load the configuration.

If you are integrating a mod with CLS then the contents of the dev directory has all you will need - an assembly that defines the interfaces, and a suggested snippet of code for testing for and accessing CLS.

Thread for discussion: http://forum.kerbalspaceprogram.com/threads/122126
Please log bugs in github if possible: https://github.com/codepoetpbowden/ConnectedLivingSpace
If you can not use github then report in the forum.

License:
--------

ConnectedLivingSpace is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

changelog:
----------
release v1.2.0.0
* Added Editor-based tweakables to allow passability customization of a part during vessel construction. Off by default. (Original CLS behavior)
* Added option to enable / disable parts not originally passable.  
* Expand and reformat parameter info in the RMB of the editor part description dialog.
* Added options window and moved options out of CLS window.
* Fixed bug with turning off Blizzy toolbar icon in Editor.

release v1.1.3.1
* Changed Unrestricted Crew Transfers option to be available when no vessel is loaded in VAB/SPH
* Fixed a bug in the Use Blizzy Toolbar option to disable the option when Blizzy Toolbar is not installed.

release v1.1.3.0
* Added trigger to overcome bug in KSP 1.0.2 that prevents stock toolbar icon from displaying.
* Added Blizzy Toolbar support with hot switching between stock and blizzy toolbars.
* Added support for KSP-AVC (if installed).
* Fixed a bug in OnVesselLoad to ensure only the active vessel is loaded into CLS.

release v1.1.2.0
* Bug fix

release v1.1.1.0
* Changes to highlighting to allow less clashing with other highlighting mods.

release v1.1.0.0
* Updated to be compatible with KSP 0.90

release v1.0.11.0
* Fixed a bug in the stock transfer code that ment it only worked for the first vessel.

release v1.0.10.0
* Add CLS support to stock transfers
* Fixed a problem of unconfigured parts with crewspace not being considered passable
* Removed Near Future config which is now shipped with Near Future

release v1.0.9.0
* built against KSP 0.25
* Added config for the new Mk spaceplane parts added to stock by porkjet.
* Added config for Near Future Spacecraft mod.
* Added config for Better Science mod
* Added config for Coffee Industries mod
* Added config for Hawkspeed Airstairs mod
* Added config for IXS mod
* Added config for KAX mod
* Added config for mk3 Nazari mod
* Added config for SH mod
* Added config for TT mod

release v1.0.8.0
* Built against KSP 0.24.2
* Updated build process to use zip utilities installed on 64 bit OS
* Changed to use the stock toolbar, and removed the dependency on blizzy's toolbar.
* Improved the configuration documentation, and made it easier to find (in the mod root directory)
* Fixed issue with the descriptions of the parts in the VAB/SPH

release v1.0.7.0
* Added support for the FASA parts pack
* removed the config for Porkworks habitat part pack as this will be shipped with the habitate pack in the future.
* Fixed bug where root part is deleted in the editor.

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
