import os
import subprocess

folder = "Resources\\FunctionalTests"
executable_path = "bin\\Debug\\net5.0\\Translator.exe"
subfolders = [ f.path for f in os.scandir(folder) if f.is_dir() ]
print(subfolders)
for subfolder in subfolders:
    with open(f"{subfolder}\\descr.txt", encoding='utf-8-sig') as description:
        print(description.read())
    input_file = f"{subfolder}\\input.py"
    with open(input_file, encoding="utf-8-sig") as input:
        print("<<<<INPUT (first 10 lines)")
        for i in range(10):
            line = input.readline()
            if line != '':
                print(line, end='')
    print(executable_path)
    output_file = f"{subfolder}\\output.cs"
    call_output = subprocess.Popen([f"{executable_path}", input_file, output_file], stdout=subprocess.PIPE).communicate()[0].splitlines()
    print(">>>>CALL OUTPUT (first 20 lines)")
    for i in range(20):
        if i < len(call_output):
            print(call_output[i].decode('utf-8'))
    print("====TRANSLATION RESULT (first 20 lines)")
    try:
        with open(output_file) as output:
            for i in range(20):
                line = output.readline()
                if line != '':
                    print(line, end='')
    except FileNotFoundError:
        pass
