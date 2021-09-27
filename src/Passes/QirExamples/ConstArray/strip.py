import sys

with open(sys.argv[1], "r") as fb:
    lines = fb.readlines()

newlines = []
for line in lines:
    if line.startswith("attributes"):
        continue

    if line.startswith("declare ") and "#" in line:
        main, leftover = line.split("#", 1)
        if "personality" in leftover:
            _, x = leftover.split("personality", 1)
            main += " personality " + x.strip()[:-1]
        newlines.append(main + "\n")
        continue

    if not line.startswith("define "):
        newlines.append(line)
        continue

    if "#" not in line:
        newlines.append(line)
        continue
    main, leftover = line.split("#", 1)

    if "personality" in leftover:
        _, x = leftover.split("personality", 1)
        main += " personality " + x.strip()[:-1]

    newlines.append(main + " {\n")


with open(sys.argv[2], "w") as fb:
    fb.write("".join(newlines))
