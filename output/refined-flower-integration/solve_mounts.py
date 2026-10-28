import json,itertools,pathlib
p=pathlib.Path(__file__).with_name('outer-mount-offsets.json');d=json.loads(p.read_text());pairs=d['pairs'];best=None
def solve(flips):
    edges=[]
    for i,s in enumerate(pairs):
        a,b=s['indices'];sign=s['sign']*(-1 if i in flips else 1)
        edges.append((a,b,s['lo']-.0002) if sign>0 else (b,a,-s['hi']-.0002))
    x=[0.]*14
    for _ in range(15):
        changed=False
        for a,b,v in edges:
            if x[a]>x[b]+v+1e-10:x[a]=x[b]+v;changed=True
        if not changed:return x
    return None
for n in range(4):
    for flips in itertools.combinations(range(len(pairs)),n):
        x=solve(flips)
        if x is None:continue
        score=-min(x)-sum(x)*.05
        if best is None or score<best[0]:best=(score,x,flips)
assert best is not None
names=['OuterPetal_01']+[f'OuterPetal_{i:02}' for i in range(3,16)]
d.update(success=True,message='Smallest mounting-depth score among up to three reversed lap joins',reversed_joins=[pairs[i]['names'] for i in best[2]],offsets_flower_m={n:v/1.68608963 for n,v in zip(names,best[1])})
p.write_text(json.dumps(d,indent=2));print(json.dumps({k:v for k,v in d.items() if k!='pairs'},indent=2))
