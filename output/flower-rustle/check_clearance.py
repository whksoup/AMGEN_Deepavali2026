"""Check true triangle separation in Unity's skinned pose snapshots."""
import ast
import gzip
import itertools
import json
import multiprocessing
from pathlib import Path
import numpy as np

HERE = Path(__file__).resolve().parent
source = ast.parse((HERE.parent / 'flower-petal-prototype/validate_outer_petal_layer.py').read_text())
functions = [n for n in source.body if isinstance(n, ast.FunctionDef) and n.name in ('clip', 'pair')]
exec(compile(ast.Module(body=functions, type_ignores=[]), 'triangle_clearance', 'exec'))
source = ast.parse((HERE.parent / 'flower-petal-prototype/validate_unity_outer_clearance.py').read_text())
exec(compile(ast.Module(body=[n for n in source.body if isinstance(n, ast.FunctionDef) and n.name == 'geometry'], type_ignores=[]), 'geometry', 'exec'))

def check(task):
    definitions, pose = task
    meshes = [geometry(d, v) for d, v in zip(definitions, pose['geometry'])]
    top = [m for m in meshes if m['layer'] == 'top']
    lower = [m for m in meshes if m['layer'] == 'outer']
    neighbors = [p for a, b in itertools.combinations(top, 2) if (p := pair(a, b, False))]
    # Check clearance against the lower ring in every pose as well.
    vertical = [p for a in lower for b in top if (p := pair(a, b, True))]
    return {
        'pose': pose['name'],
        'top_neighbor_min_gap_mm': 1000 * min(p['min_gap_m'] for p in neighbors),
        'top_neighbor_crossings': sum(p['crossing_triangle_pairs'] for p in neighbors),
        'lower_layer_min_gap_mm': 1000 * min(p['min_gap_m'] for p in vertical),
        'lower_layer_crossings': sum(p['crossing_triangle_pairs'] for p in vertical),
        'worst_top_pair': min(neighbors, key=lambda p: p['min_gap_m'])['pair'],
    }

if __name__ == '__main__':
    snapshot_path = HERE / 'pose-snapshots.json'
    if snapshot_path.exists():
        data = json.loads(snapshot_path.read_text(encoding='utf-8-sig'))
    else:
        with gzip.open(HERE / 'pose-snapshots.json.gz', 'rt', encoding='utf-8-sig') as stream:
            data = json.load(stream)
    results = []
    with multiprocessing.Pool(4) as pool:
        for result in pool.imap(check, [(data['meshDefinitions'], p) for p in data['poses']]):
            results.append(result)
            if len(results) % 8 == 0:
                print(f'Checked {len(results)}/{len(data["poses"])} poses', flush=True)
    summary = {
        'poses': len(results),
        'top_neighbor_min_gap_mm': min(p['top_neighbor_min_gap_mm'] for p in results),
        'top_neighbor_crossings': sum(p['top_neighbor_crossings'] for p in results),
        'lower_layer_min_gap_mm': min(p['lower_layer_min_gap_mm'] for p in results),
        'lower_layer_crossings': sum(p['lower_layer_crossings'] for p in results),
        **data['checks'],
    }
    summary['passed'] = (summary['top_neighbor_crossings'] == summary['lower_layer_crossings'] == 0
                         and summary['top_neighbor_min_gap_mm'] > 0
                         and summary['lower_layer_min_gap_mm'] > 0
                         and summary['maxRootDrift'] < 1e-6
                         and summary['maxBoundsOverflow'] < 1e-6
                         and summary['maxSyncError'] < 1e-5
                         and summary['maxLocalPositionDrift'] < 1e-6
                         and summary['maxLocalScaleDrift'] < 1e-6)
    (HERE / 'clearance-validation.json').write_text(json.dumps({
        'summary': summary,
        'method': 'Exact vertical plane separation over clipped projected triangle overlaps from Unity BakeMesh; discrete rest, idle, pulse and opposing-envelope poses.',
        'poses': results,
    }, indent=2))
    print(json.dumps(summary, indent=2), flush=True)
    raise SystemExit(0 if summary['passed'] else 1)
