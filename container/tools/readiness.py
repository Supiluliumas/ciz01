#!/usr/bin/env python3
"""Offline configuration check. Never prints configuration or secret values."""
import argparse
import json
import os
import re
from pathlib import Path
from urllib.parse import urlsplit

# Core keys are required to deploy at all. Each integration group is
# all-or-nothing: the app reports the feature as 'not-configured' until the
# whole group is present, so a partially filled group deploys but stays off.
CORE = ('GCP_PROJECT_ID', 'VERTEX_MODEL')
GROUPS = {'email': ('N8N_WEBHOOK_URL', 'N8N_AUTH_TOKEN'),
          'test_data': ('BIGQUERY_PROJECT_ID', 'BIGQUERY_DATASET', 'BIGQUERY_TABLE')}
INTEGRATION_KEYS = tuple(key for keys in GROUPS.values() for key in keys)
REQUIRED = CORE + INTEGRATION_KEYS
ENV_KEYS = REQUIRED + ('VIBE_LOCAL', 'VERTEX_LOCATION', 'N8N_HEADER_NAME', 'BOOTSTRAP_ADMIN_EMAIL')


def assess(config_path, environment):
    file_values = {}
    issues = []
    if not config_path.is_file():
        issues.append('Configuration file is missing.')
    else:
        for line in config_path.read_text().splitlines():
            line = line.strip()
            if not line or line.startswith('#'):
                continue
            if '=' not in line:
                issues.append('Malformed configuration line (value omitted).')
                continue
            key, value = line.split('=', 1)
            key, value = key.strip(), value.strip()
            if key in file_values:
                issues.append(f'Duplicate configuration key: {key}')
            file_values[key] = value
            if value and re.search(r'TOKEN|PASSWORD|SECRET|API_KEY|CREDENTIAL', key):
                issues.append('Secret-like configuration must be removed from the file; use Secret Manager.')
    values = dict(file_values)
    values.update({key: value for key, value in environment.items() if key in ENV_KEYS})
    checks = [{'key': key, 'present': bool(values.get(key, '').strip())} for key in REQUIRED]
    if values.get('VIBE_LOCAL') == '1' or 'VIBE_LOCAL' in file_values:
        issues.append('Deployment must not enable local simulation or store VIBE_LOCAL in config.env.')
    for key in ('GCP_PROJECT_ID', 'BIGQUERY_PROJECT_ID', 'BIGQUERY_DATASET', 'BIGQUERY_TABLE'):
        value = values.get(key, '')
        if value and not re.fullmatch(r'[a-zA-Z0-9_-]+', value):
            issues.append(f'Invalid resource identifier: {key}')
    model = values.get('VERTEX_MODEL', '')
    if model and not re.fullmatch(r'[a-zA-Z0-9_.-]+', model):
        issues.append('Invalid resource identifier: VERTEX_MODEL')
    url = values.get('N8N_WEBHOOK_URL', '')
    if url:
        try:
            parsed = urlsplit(url)
            if parsed.scheme != 'https' or not parsed.hostname or parsed.username or parsed.password:
                issues.append('N8N_WEBHOOK_URL must be HTTPS without embedded credentials.')
            elif parsed.hostname.endswith(('.test', 'example.com')) or '/webhook-test/' in parsed.path:
                issues.append('N8N_WEBHOOK_URL must be the published production webhook, not an example or test URL.')
        except ValueError:
            issues.append('N8N_WEBHOOK_URL is invalid.')
    header = values.get('N8N_HEADER_NAME') or 'X-N8N-Token'
    if not re.fullmatch(r'[A-Za-z0-9-]+', header) or header.lower() in ('authorization', 'cookie', 'host', 'content-type', 'content-length'):
        issues.append('N8N_HEADER_NAME must be a custom header for Header Auth.')
    token = values.get('N8N_AUTH_TOKEN', '')
    if '\r' in token or '\n' in token:
        issues.append('N8N_AUTH_TOKEN contains invalid line breaks.')
    missing = [key for key in CORE if not values.get(key, '').strip()]
    integrations = {}
    for name, keys in GROUPS.items():
        present = [bool(values.get(key, '').strip()) for key in keys]
        integrations[name] = 'configured' if all(present) else 'partial' if any(present) else 'not-configured'
    return {'ready_configuration': not missing and not issues, 'checks': checks,
            'missing_keys': missing, 'integrations': integrations, 'issues': issues,
            'live_verification': 'Pending: n8n secret mount, BigQuery table grant, Vertex response, n8n execution, and inbox receipt. This check performs no cloud requests.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', type=Path, default=Path(__file__).resolve().parents[1] / 'config.env')
    parser.add_argument('--strict', action='store_true', help='Exit 2 if configuration is incomplete.')
    parser.add_argument('--json', action='store_true')
    args = parser.parse_args()
    report = assess(args.config, os.environ)
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print('Configuration ready' if report['ready_configuration'] else 'Configuration not ready for deployment')
        for check in report['checks']:
            print(('OK      ' if check['present'] else 'MISSING ') + check['key'])
        for name, state in report['integrations'].items():
            print(f'{state.upper():<15}integration: {name}')
        for issue in report['issues']:
            print('ERROR   ' + issue)
        print(report['live_verification'])
    return 2 if args.strict and not report['ready_configuration'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
