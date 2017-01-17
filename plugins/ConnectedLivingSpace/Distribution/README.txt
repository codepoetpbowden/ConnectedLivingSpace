Connected Living Space v1.2.4.1
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
release v 1.2.4.2
* New: Added a custom event to notify mods that the CLS vessel data has been refreshed.

release v 1.2.4.1
* Fixed: Some parts were not merging spaces event when hatches were opened. Github Issue #75. Forum Post: http://forum.kerbalspaceprogram.com/index.php?/topic/109972-122-connected-living-space-v1240-30-dec-2016-customize-your-cls-parts/&do=findComment&comment=2906269
* Fixed: CLSDefaultPart.cfg was included in distribution.  There should only be a CLSDefaultPart.cfg.txt file.  Removed.  Github Issue #78.

release v 1.2.4.0
* New:  Refactored to support KSP 1.2.2.
* New:  Completely refactored method used to Add hatches to vessels.  Now utilizes a module manager config, eliminating prefab manipulation in game.
* New:  Code refactored to improve performance and garbage collection.
* Fixed: Some parts containing ModuleDockingNode without a referenceNodeName would be rendered impassable in some nodes.
* Fixed: NRE generated during Vessel load. The addition of a female kerbal broke the CLS Module attachment code when a vessel is loaded at Flight. 
         (This was a old undetected bug, that may explain some parts not showing as passble)
* Fixed: Spammed Index out of range error during space changes while CLS Window is opened.

release v 1.2.3.0
* New:  Added support for intercepting Parts selection list during stock Transfer target part selection.  A part not in the same space will be unselectable and is highlighted orange like full parts.
* New:  Added support for overriding the "Allow unrestricted Crew Transfers"in CLSInterfaces.dll setting via other Mods to prevent "competition" between mods when handling stock crew transfers.
* New:  Updated config for Docking Port Jr.  Squad now says that a kerbal can squeeze thru.
* New:  Refactored code to improve performance, recuce garbage collection, & use Explicit typing.
* Fixed: CLS windows now properly close when changing scenes.
* Fixed: In the Editor, part highlighting does not work correctly when adding new crewable parts.

release v 1.2.2.1
* Fixed:  Stock Crew Transfer fails for "not in same space" even when the 2 parts are in the same space.

release v 1.2.2.0
* New:  Refactored Stock Crew Transfer Handler to use new KSP 1.1.3 events to pre-empt the transfer if disallowd by CLS.

release v 1.2.1.5
* Fixed:  Finally squashed NullRef exceptions when RemoteTech is installed.
* New:  Added Distribution folder to project for ease in locating binaries from Git.
* New:  Added folder check for PluginData to ensure proper config file creation when Mod is installed.

release v 1.2.1.4
* Fixed:  NullRef exceptions when RemoteTech is installed.
* New:  Moved configuration file from GameData root folder to GameData\ConnectedLivingSpace\Plugins\PluginData folder to comply with KSP folder standards for mods.

release v 1.2.1.3
* New:  Changed behavior of CLSClient.cs (API wrapper class) to prevent additional assembly scans when called. Ref Git Issue #72.
* New:  Added new configs for Taurus HCV.  Git Issue #71
* New:  Added config changes for KOSMOS SSPP  Git Issue #69
* New:  Refactoring for KSP 1.1.2 (WIP)

release v 1.2.1.2
* Fixed:  WHen a Stock Crew transfer is overridden, the override message is not properly dislayed.
* Fixed:  WHen a Stock Crew transfer is overridden, the original move message is not properly removed.

release v 1.2.1.1
* Fixed:  Null reference errors.
* Fixed:  Window would not open

release v 1.2.1.0
* New:  Updated mod for KSP 1.1.2 compatability.

prerelease v 1.2.0.9
* New:  Updated mod for KSP 1.1 compatability.
* New:  Corrected Stock Screen Messages so that they are properly removed when CLS overrides a Stock Crew Transfer.
* Fixed:  CLS would not display a window when the stock Icon was clicked.
* Fixed:  CLS should now only display 1 icon in Editor or flight.  Removed redundant icon call in Start, now that stock buttons now behave as intended.

release v1.2.0.2
* New:  Added Changes to configurations based on conversations in forums and a Pull Requests by Technologicat, khr15714n &  Kerbas-ad-astra.
* Fixed:  Correct build deploy automation to project (missing icons for blizzy). 
* Fixed:  CLS tweakables incorrectly visible when custom passability is disabled.

release v1.2.0.1
* Add build deploy automation to project. 
* Correct deploy error resulting in incorrect dll build being released.

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
