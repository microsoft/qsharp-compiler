# Qubit Allocation Analysis

## Purpose

The purpose of this pass is to analyse the code for qubit allocations and identify
the allocation dependency. This helps subsequent transfomation passes expand the code
to, for instance, eliminate loops and classical logic. This is desirable as the control
logic for some quantum computing systems may be limited and one may therefore wish
to reduce its complexity as much as possible at compile time.
