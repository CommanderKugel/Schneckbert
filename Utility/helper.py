import time

folder_path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\Ressources\\self_play_data\\"

file_names = [
    "Selfplaydata_24-11-24_0", 
    "Selfplaydata_24-11-24_1", 
    "Selfplaydata_24-11-24_2", 
    "Selfplaydata_24-11-24_3", 
    "Selfplaydata_25-11-24_1", 
    "Selfplaydata_25-11-24_2", 
    "Selfplaydata_25-11-24_3", 
    "Selfplaydata_25-11-24_4", 
    "Selfplaydata_25-11-24_5",
    "Selfplaydata_25-11-24_6",
    "Selfplaydata_25-11-24_7",
]

start_time = time.time()

with open(folder_path+"Selfplaydata_big.txt", "a") as big:

    for name in file_names:
        with open(folder_path+name+".txt", "r") as smol:
            
            line = smol.readline()
            while line:
                
                big.write(line)
                line = smol.readline()
                
            big.write("\n")
        
        print("done with "+name)
        print("this took", time.time()-start_time, "s")

print("Done completely!")
print("finished in", time.time()-start_time, "s")
