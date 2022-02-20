---
title: Integration with assemblers
weight: 40
pre: ""
chapter: false
---

When building homebrew software in assembly or C, it is possible to export the labels used in your code and import them into Mesen-S to simplify the debugging process.
This allows the debugger to know which portions of the ROM correspond to which functions in your code, as well as display your code's comments inside the debugger itself.

## Integration with assemblers ##

Mesen-S includes built-in support for a number of different debug symbol formats. These can be imported to provide additional information during debugging, such as label names, source views and more, depending on the symbols format.

To import a debug symbol file, use the **<kbd>File&rarr;Workspace&rarr;Import Labels</kbd>** command in the debugger window. You can also enable the `Automatically load debug symbol files` option in **<kbd>File&rarr;Import/Export&rarr;Integration Settings</kbd>** to make Mesen-S load any debug symbol file it finds next to the ROM whenever the debugger is opened.  
**Note:** For this option to work, the ROM file must have the same name as the symbols file (e.g `MyRom.sfc` and `MyRom.dbg`) and be inside the same folder.

The `Automatically load debug symbol files` option prioritizes debug symbol files in the following order:

1. `.dbg` files
2. `.msl` files
2. `.sym` files

When loading a `.sym` file, Mesen-S automatically tries to guess the type of `.sym` file based on its contents.

#### Source View ####

<div class="imgBox"><div>
	<img src="/images/SourceView.png" />
	<span>Source View</span>
</div></div>

When certain types of symbol files are loaded, an additional option appears in the code window's right-click menu:

* **Switch to Source View**: This turns on `Source View` mode, which allows you to debug the game using the original code files, rather than the disassembly.  This can be used for both assembly and C projects.

### CC65 / CA65 ###
 
Integration with CC65/CA65 is possible via `.dbg` files.
To make CC65/CA65 create a `.dbg` file during the compilation, use the `--dbgfile` command line option.

Source View is supported for `.dbg` files.

### WLA-DX ###

Integration with WLA-DX is possible via `.sym` files.

Source View is supported for WLA-DX `.sym` files.

### bass ###

Integration with bass is possible via `.sym` files.

### RGBDS ###

Integration with RGBDS (for Game Boy projects) is possible via the `.sym` files that RGBDS produces.

## Importing and exporting labels ##

<div class="imgBox"><div>
	<img src="/images/ImportExportMenu.png" />
	<span>Import/Export</span>
</div></div>

Mesen-S can also import and export labels in `.msl` format. The ability to import labels can be used to integrate the debugger with your own workflow (e.g by creating your own scripts that produce `.msl` files)

<div style="clear:both"></div>

### Mesen-S Label Files (.msl) ###

The `.msl` files used by Mesen-S to import/export labels is a simple text format.  For example, this defines a label and comment on byte $100 of PRG ROM:
```
PRG:100:MyLabel:This is a comment
```
The format also supports multi-byte labels, defined by giving specifying an address range:
```
PRG:200-2FF:MyArray
```

The first part on each row is used to specify the label's type:
```
PRG: PRG ROM labels
WORK: Work RAM labels (for the SNES' internal 128kb Work RAM)
SAVE: Save RAM labels
REG: Register labels
SPCRAM: SPC RAM labels
SPCROM: SPC IPL ROM labels
IRAM: SA-1 IRAM labels
PSRAM: BS-X PS RAM labels
MPACK: BS-X Memory Pack labels
DSPPRG: DSP Program ROM labels
GBPRG: Game Boy Program ROM labels
GBWRAM: Game Boy Work RAM labels
GBSRAM: Game Boy Cart/Save RAM labels
GBHRAM: Game Boy High RAM labels
GBBOOT: Game Boy Boot ROM labels
GBREG: Game Boy Register labels
```

### Integration Settings ###

<div class="imgBox"><div>
	<img src="/images/IntegrationSettings.png" />
	<span>Integration Settings</span>
</div></div>

For fine-grain control over the debug symbol file imports, the `Integration Settings` ( **<kbd>File&rarr;Import/Export&rarr;Integration Settings</kbd>**) window can be used.  
This allows you to configure which types of labels/comments should be imported, as well as choosing whether or not Mesen-S should delete all existing labels before importing debug symbol files.