AGC-DSKY emulation for KSP

This is mostly work in progress, and I take time to discover C# and AGC/DSKY, the later not the easier.

I'll upload ASAP some assembly information to the wiki (to play with the assembler), and important information about the memory map, register, files and so on.

Notice that it isn't an accurate simulation of the AGC (I'll use some differents OPCODES, and the YUL will be interpreted by the plugin, not by the Interpreter hardcoded into the AGC), but I'll try to stay as close as possible to the original design (memory map, register, clock...)

Some quick specification :
16b words 
8 * 256 words of RAM (2kw)
36 * 1024 words of ROM (36kw)
12µs / cycle
about 30 AGC instructions + YUL interpreted language.
2 * 4 words I/O registers

As you can see, this is very very small memory to perform many calculation, so optimisation and compacting are expected.

05/10/2013 : 
Memory map access and file : DONE (Bank class)
Word manipulation : DONE (sWord class)
Clock emulation : TODO (placeholder CLOCK class)
AGC : TODO (bases are set, interpreter should work. OPCODES need to be coded)
ASSEMBLER : IN PROGRESS (I wish to start by the assembler, so I have fully runable code to test the AGC, and coding series of 0 and 1 in a 38.000 lines long text file is... Well...)

For now, the assembler routine recognize TC/AD/MASK (for testing purpose), labelled adress (as defined constant), in-code adress (as jump target), but doesn't switch bank yet (to come soon, working on it...) and doesn't do anything more.... Except compiling it to bitcode for the AGC to interpret them (wich it does, by displaying opcodes interpreted and the operand)

Any suggestions welcome. The code is "documented" and may be understandable, but any question welcome too :)