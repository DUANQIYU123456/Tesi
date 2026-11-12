from pathlib import Path
import math,random,wave,struct
random.seed(142);rate=24000;data=[];filtered=0
for i in range(int(rate*.48)):
 t=i/rate;filtered=.80*filtered+.20*random.uniform(-1,1)
 envelope=math.exp(-((t-.06)/.018)**2)+.7*math.exp(-((t-.22)/.055)**2)
 value=(filtered*.7+math.sin(2*math.pi*(190*t-95*t*t))*.15)*envelope*.45
 data.append(struct.pack('<h',int(value*32767)))
with wave.open(str(Path(__file__).with_name('Lip_Focus_Prototype.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(b''.join(data))

