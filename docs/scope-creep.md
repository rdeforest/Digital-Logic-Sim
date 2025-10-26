I'm going to creep the scope. I want to either find or create a language for
describing circuits and add a tool for generating circuits from that language.
My thinking is that I like having you create circuits but I want a more robust
flow for it. Is there a language already in use that is as succinct as
CoffeeScript? If not I'd like to use CoffeeScript as the base language so I
could describe a circuit something like this:

```coffee
(require 'circuit') (circuit) ->
  circuit name: 'NOT', (chip, wire) ->
    in0   = chip.in1()
    NAND0 = chip.nand()
    out0  = chip.out1()

    wire in0  .out[0], NAND0.in[0], NAND0.in[1]
    wire NAND0.out[0], out0 .in[0]

  circuit name: 'AND', (chip, wire) ->
    in0   = chip.in1()
    NAND0 = chip.nand()
    NAND1 = chip.nand()
    out0  = chip.out1()

    wire in0  .out[0], NAND0.in[0], NAND0.in[1]
    wire NAND0.out[0], NAND1.in[0], NAND1.in[1]
    wire NAND1.out[0], out0 .in[0]
```

The export of 'circuit' is a function which takes a circuit-defining function
as its one parameter.

The circuit function takes an optional options object and a mandatory callback
function which performs the actual circuit construction.

The callback takes two parameters: one for creating chips (parts) and one for
creating wires. The chip creator is an object whose key-value pairs are
functions for constructing various parts. The part constructors take an
optional config for defining shapes and colors.

```coffee
chip.getSubchip = (details) ->
  for finder in subchipFinders
    if found = finder details
      return found details

  return undefined
```
