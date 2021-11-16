import os
import json

path = os.path.dirname(__file__)

# Creating stats
for d in os.listdir(path):
    if os.path.isdir(d):
        if os.path.exists(os.path.join(d, "validation.json")):
            continue
        os.chdir(d)
        os.system("make")
        os.chdir(path)


all_data = []
for d in os.listdir(path):
    if os.path.isdir(d):
        filename = os.path.join(d, "validation.json")
        if not os.path.exists(filename):
            continue

        with open(filename, "r") as fb:
            contents = fb.read()
            data = json.loads(contents)

        all_data.append((len(data), d, data))


opcounts = {

}

all_data.sort()
for c, d, data in all_data:
    print(d.ljust(40), c)

    for err in data:
        err = err["message"]
        if "'" in err:
            _, op, _ = err.split("'", 2)
            if op not in opcounts:
                opcounts[op] = {"count": 0, "op": op, "used_by": []}

            opcounts[op]["count"] += 1
            opcounts[op]["used_by"].append(d)

print()
opcodes = [(b["count"], name, b) for name, b in opcounts.items()]
opcodes.sort()
for count, name, op in opcodes:
    print(op["op"].ljust(40), count, op["used_by"])
