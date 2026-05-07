#!/usr/bin/env python3
import json
import os
import sys
import requests

CONFIG_PATH = 'returnalphaboost.config.json'

def load_config(path):
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

def collect_urls(cfg):
    profiles = cfg.get('profiles') or {}
    results = []
    for pname, pdata in profiles.items():
        mappings = pdata.get('mappings') or []
        for m in mappings:
            src = m.get('source')
            if src:
                results.append((pname, src))
    return results

def check_url(session, profile, url):
    try:
        resp = session.head(url, allow_redirects=True, timeout=10)
        if resp.status_code < 400:
            print(f'OK   {profile} {url} ({resp.status_code} HEAD)')
            return True, None
        # HEAD returned error; try a lightweight GET
        resp = session.get(url, stream=True, timeout=10)
        if resp.status_code < 400:
            print(f'OK   {profile} {url} ({resp.status_code} GET)')
            return True, None
        print(f'FAIL {profile} {url} HTTP {resp.status_code} {resp.reason}')
        return False, f'HTTP {resp.status_code} {resp.reason}'
    except requests.RequestException as e:
        print(f'ERR  {profile} {url} {e}')
        return False, str(e)

def main():
    if not os.path.exists(CONFIG_PATH):
        print(f'Config file not found: {CONFIG_PATH}')
        return 2

    cfg = load_config(CONFIG_PATH)
    urls = collect_urls(cfg)
    if not urls:
        print('No URLs found in config.')
        return 0

    session = requests.Session()
    session.headers.update({'User-Agent': 'ReturnAlphaBoost-URL-Checker/1.0'})

    failures = []
    for profile, url in urls:
        ok, err = check_url(session, profile, url)
        if not ok:
            failures.append((profile, url, err))

    print('\nSummary:')
    print(f'  Total URLs: {len(urls)}')
    print(f'  Failures:   {len(failures)}')
    if failures:
        print('\nFailed entries:')
        for p, u, e in failures:
            print(f'- {p} {u} -> {e}')
        return 1

    return 0

if __name__ == '__main__':
    code = main()
    sys.exit(code)
