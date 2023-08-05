# [EA] MoveCost2EA

## What is this?

MoveCost2EA is an attempt at making a more buildfile-friendly method of editing movement costs. Using it, you can define 
new move cost tables as variants of previously defined ones, to avoid having to write out the full move costs every time when the only change is something slight.

## Basic Format

The basic format of a MoveCosts2EA script is as follows. Define a new movegroup with `MyNewMovegroup {`. Set the values at different indices within the movegroup with however many lines of the format `index = value`, where `index` is the index within the table you want to set and `value` is the value you want to set at that index. Once you've defined everything you want to, end the movegroup with `}`.

## Importing Movegroups

When defining a movegroup, you can import another movegroup and the new one will begin with all values equal to the old one, while keeping the old one intact. To do this, define the movegroup as `MyNewMovegroup imports MyOtherMovegroup {`.

## Value Operations

You can perform a series of operations on existing values at indexes. `index + value`, `index - value`, `index * value`, and `index / value` will add, subtract, multiply, or divide, respectively, the value currently at `index` with `value` and store the result to `index`.

## Range Operations

You can perform an operation on a range of indices and values at once. `[index1,index2,index3] = value` will set the values at all 3 mentioned indices to `value`. `[index1-index5] = value` will set the values at every index from `index1` to `index5`, inclusive, to `value`. `value` can also be a range in either of the same formats, but if `value` is an array, the lengths of the `input` and `value` arrays must be identical. Each value in `value`'s array gets mapped to each value in `index`'s array. All operands support range operations.

## Directly Referencing Other Tables

You can directly reference one or more specific values in another movegroup from within the current one. `index1 = MyOtherMovegroup[index2]` will set the value at `index1` in the current movegroup to the value at `index2` in `MyOtherMovegroup`. Range operations are supported. You cannot write to another movegroup from within the current one, only read from it.

## Definitions

Whenever you are not within a movegroup definition, you can define aliases for values. `#define name value` will allow you to use `name` in place of instances of `value`. For example, `#define Empty 0` means you can write `Empty` instead of `0` for the index in a movegroup definition. You can use existing definitions for `value` and it will parse until it finds a valid integer.

## Includes

You can include other files from the one passed to the program with the syntax `#include Filepath`.

## Comments

Single-line comments are supported. Anything on a line starting from an instance of `//` is ignored.

## Usage

The syntax for using the program is `MoveCost2EA <inputFilename> [outputFilename]`. If you do not supply a value for `outputFilename`, it will default to `MoveCostsInstaller.event`.

