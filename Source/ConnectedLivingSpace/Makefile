KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
CLSGAMEDATA  := ${GAMEDATA}/ConnectedLivingSpace
PLUGINDIR	:= ${CLSGAMEDATA}/Plugins

TARGETS		:= ConnectedLivingSpace.dll

CLS_FILES := \
	CLSAddon.cs	\
	CLSKerbal.cs	\
	CLSPart.cs	\
	CLSSpace.cs	\
	CLSStyles.cs	\
	CLSVessel.cs	\
	CLSVesselModule.cs	\
	ModuleConnectedLivingSpace.cs	\
	ModuleDockingHatch.cs	\
	Properties/AssemblyInfo.cs	\
	ToolbarWrapper.cs	\
	$e

DOC_FILES := \
	../License.txt \
	$e

#	ConnectedLivingSpace.png
#	README.md

RESGEN2		:= resgen2
GMCS		:= mcs
#GMCSFLAGS	:= -optimize -warnaserror
GMCSFLAGS	:= -optimize
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: ${TARGETS} #ConnectedLivingSpace.png

.PHONY: version
version:
	@../tools/git-version.sh

info:
	@echo "ConnectedLivingSpace Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

ConnectedLivingSpace.dll: ${CLS_FILES}
	${GMCS} ${GMCSFLAGS} -t:library -lib:${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass \
		-r:UnityEngine,UnityEngine.UI \
		-lib:${PLUGINDIR} -r:CLSInterfaces \
		-out:$@ $^

#ConnectedLivingSpace.png: ConnectedLivingSpace.svg
#	inkscape --export-png $@ $^

clean:
	rm -f ${TARGETS} AssemblyInfo.cs #ConnectedLivingSpace.png

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}
#	cp ${DOC_FILES} ${CLSGAMEDATA}

.PHONY: all clean install
