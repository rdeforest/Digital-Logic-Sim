# Base

wireTruth = {
}

permute = (n) ->
  for i in [0 .. 2**n - 1]
    i .toString 2
      .padStart n, '0'
      .split ''
      .map (value) -> not not parseInt value

nand = (a, b) ->
  states =
  ( for inputs, idx in permute 4
      [first, second] = [a, b].map (param) ->
        if 'string' is typeof param
          '1' is param[idx]
        else
          inputs[pinsIn.indexOf param]

      #console.log {first, second}
      if not (first and second)
        '1'
      else
        '0'
  ).join ''

pinsIn = [
  Sub = Symbol 'Sub'
  A   = Symbol 'A'
  B   = Symbol 'B'
  Cin = Symbol 'Cin'
]

gateOutputs =
  N1  : N1 = nand A,   Sub
  N2  : N2 = nand A,   N1
  N3  : N3 = nand Sub, N1
  N4  : N4 = nand N2,  N3

  N5  : N5 = nand N4,  B

  N6  : N6 = nand A,   B
  N7  : N7 = nand A,   N6
  N8  : N8 = nand B,   N6
  N9  : N9 = nand N7,  N8

  N10 : N10 = nand N9,  Sub
  N11 : N11 = nand N9,  nand N9, Sub
  N12 : N12 = nand Sub, nand N9, Sub
  N13 : N13 = nand N11, N12

  N14 : N14 = nand N9,  Cin
  N15 : N15 = nand N9,  nand N9, Cin
  N16 : N16 = nand Cin, nand N9, Cin
  N17 : N17 = nand N15, N16

  N18 : N18 = nand N13, Cin

## Carry
  N19 : N19 = nand N5,  N18

for name, states of gateOutputs
  if prev = wireTruth[states]
    #console.log "#{name} == #{prev} (#{states})"
    name
  else
    wireTruth[states] = name

