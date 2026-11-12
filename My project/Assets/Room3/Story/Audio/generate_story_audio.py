from pathlib import Path
import math, random, struct, wave

out=Path(__file__).parent/'audio';out.mkdir(exist_ok=True)
rate=24000
def make(name,seconds,kind):
    rng=random.Random(1928+kind); low=0.; samples=bytearray()
    interval={0:.25,1:.19,2:.093,3:1.2}.get(kind,1)
    for i in range(int(seconds*rate)):
        t=i/rate;n=rng.uniform(-1,1);low=.97*low+.03*n
        if kind==0:
            y=sum(.1*math.exp(-((t+offset)%period)*130)*math.sin(2*math.pi*freq*((t+offset)%period)) for period,offset,freq in [(.5,0,1800),(.4,.13,2400),(.6,.19,1400)])
        elif kind==1:
            a=t%.19;b=(t+.073)%.37;y=.17*math.exp(-a*240)*(n*.4+math.sin(2*math.pi*2100*a))+.07*math.exp(-b*180)*math.sin(2*math.pi*1700*b)
        elif kind==2:
            a=t%.093;b=(t+.041)%.17;y=(.15*math.exp(-a*350)+.12*math.exp(-b*500))*n
        elif kind==3:
            a=t%1.2;b=(t-.19)%1.2;y=.18*math.exp(-a*25)*math.sin(2*math.pi*52*a)+.1*math.exp(-b*32)*math.sin(2*math.pi*65*b)
        elif kind==4:
            y=low*.28*math.sin(math.pi*t/seconds)+.2*math.exp(-max(0,t-.3)*8)*(math.sin(2*math.pi*68*t)+.25*n)
        elif kind==5:
            y=.26*math.exp(-t*8)*math.sin(2*math.pi*94*t)+.13*math.exp(-t*5)*math.sin(2*math.pi*310*t)+.07*math.exp(-t*4)*math.sin(2*math.pi*1700*t)+n*.08*math.exp(-t*20)
        else:
            y=low*.35*math.sin(math.pi*t/seconds)+.045*math.sin(2*math.pi*(520*t+260*t*t))*math.sin(math.pi*t/seconds)
        fade=min(1,t/.008,(seconds-t)/.025)
        samples.extend(struct.pack('<h',int(max(-1,min(1,y*max(0,fade)))*32767)))
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(samples)
for name,duration,kind in [('Layered_Ticking',12,0),('Mouse_Residue',12,1),('Geiger_Trace',12,2),('Core_Heartbeat',12,3),('Bridge_Lock',2.5,4),('Testimony_Impact',2,5),('Archive_Reveal',.65,6)]:
    make(name,duration,kind)
print('7 original audio prototypes generated')
