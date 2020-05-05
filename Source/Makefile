KSPDIR		?= ${HOME}/Source/KSP/KSP_Dev
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
CLSGAMEDATA	:= ${GAMEDATA}/ConnectedLivingSpace
PLUGINDIR	:= ${CLSGAMEDATA}/Plugins

TARGETS		:= ConnectedLivingSpace.dll CLSInterfaces.dll

rwildcard=$(wildcard $1$2) $(foreach d,$(wildcard $1*),$(call rwildcard,$d/,$2))

#CLS_FILES := $(wildcard ConnectedLivingSpace/**/*.cs)
CLS_FILES := $(call rwildcard,ConnectedLivingSpace/,*.cs)

FOO1 := \
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

CLSI_FILES := $(call rwildcard,CLSInterfaces/,*.cs)

FOO2 := \
	CLSClient.cs \
	Interfaces.cs \
	$e

DOC_FILES := \
	../../CHANGELOG.md.txt     \
	../../CONTRIBUTORS.md.txt  \
	../../License.txt          \
	../../README.md.txt        \
	$e

#	ConnectedLivingSpace.png
#	README.md

RESGEN2		:= resgen2
GMCS		:= mcs
#GMCSFLAGS	:= -optimize -warnaserror
GMCSFLAGS	:= -optimize
GIT		:= git
TAR		:= tar
ZIP		:= zip

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

CLS_LIBS := \
	Assembly-CSharp \
	Assembly-CSharp-firstpass \
	UnityEngine \
	UnityEngine.AnimationModule \
	UnityEngine.CoreModule, \
	UnityEngine.IMGUIModule \
	UnityEngine.TextRenderingModule \
	UnityEngine.UI \
	$e

CLSI_LIBS := \
	Assembly-CSharp \
	UnityEngine.CoreModule, \
	UnityEngine.UI \
	$e

comma := ,
empty :=
space := ${EMPTY} ${EMPTY}
CLS_LIBS_LIST = $(subst ${space},${comma},$(strip $(CLS_LIBS)))
CLSI_LIBS_LIST = $(subst ${space},${comma},$(strip $(CLSI_LIBS)))

ConnectedLivingSpace.dll: ${CLS_FILES} CLSInterfaces.dll
	${GMCS} ${GMCSFLAGS} -t:library \
		-lib:${MANAGED} -r:${CLS_LIBS_LIST} \
		-lib:. -r:CLSInterfaces \
		-out:$@ ${CLS_FILES}

CLSInterfaces.dll: ${CLSI_FILES}
	${GMCS} ${GMCSFLAGS} -t:library \
		-lib:${MANAGED} -r:${CLSI_LIBS_LIST} \
		-out:$@ ${CLSI_FILES}

#ConnectedLivingSpace.png: ConnectedLivingSpace.svg
#	inkscape --export-png $@ $^

clean:
	rm -f ${TARGETS}

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}
#	cp ${DOC_FILES} ${CLSGAMEDATA}

.PHONY: all clean install
