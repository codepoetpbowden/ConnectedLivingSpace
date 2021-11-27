Contributing to Connected Living Spaces
=======================================

Before contributing towards CLS, you should have a read of the License.txt file
which can be found in the root directory of the source tree. You must agree to
the license terms and conditions before contributing towards this mod.

Bug reports, feature requests, and additional configurations
------------------------------------------------------------
The easiest way to contribute is by playing KSP with CLS installed and raising
bug reports or feature requests for any issues found while playing. A good bug
report includes enough information for the developers to be able to reproduce
the problem.

Bug reports and feature requests should be reported on
[GitHub](https://github.com/mwerle/ConnectedLivingSpace/issues) but if you
don't have access to that the forum thread is also ok.

Additional configurations for mods not yet supported by CLS are always welcome.

Translations
------------
If you speak one of the languages supported by KSP, a good way to contribute is
to review the existing translation files for any errors or improvements, or by
writing a new translation.

The languages currently suported are:
 - english
 - german
 - italian
 - russian
 - spanish

 The translation resource files can be found here:
  - `Distribution/GameData/ConnectedLivingSpace/Localization/`

Developing
----------
The most ambitious way to contribute is by helping to code new features for CLS
or by fixing some of the reported bugs. To do this you need a development
environment suitable for KSP mod development.

All of the assets for this mod are kept in
[Github](https://github.com/mwerle/ConnectedLivingSpace) and you should create
a personal fork before starting any work.

The source code can be found in the `Source` folder. The primary development
environment is Visual Studio, and a Visual Studio solution file is in the
top-level folder of the source tree.

To adjust to your local development environment, copy the
`user_settings.props.template` to
`user_settings.props` and edit accordingly.

Helper scripts to deploy and release CLS are in the `Scripts` folder and
require `msbuild` v15.8 or later.

Versioning is perfmormed via the ConnectedLivingSpace `AssemblyVersion` value
which is automatically propagated into the Changelog and mod version file
when building a release package.

Please adhere to the existing coding style in this mod. To help, a Editor
Config file is provided and using a code editor which supports this is highly
recommended.

Once you have finished coding a new feature or fixing a bug, issue a pull
request and a maintainer will review the code before merging it into the code
base.
