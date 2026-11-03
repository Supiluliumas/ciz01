import json
import tempfile
import unittest
from pathlib import Path
from readiness import assess

class ReadinessTests(unittest.TestCase):
    def assess(self, content, env=None):
        with tempfile.TemporaryDirectory() as folder:
            config = Path(folder) / 'config.env'
            config.write_text(content)
            return assess(config, env or {})
    def test_empty_config_reports_dependencies_without_guessing(self):
        report = self.assess('GCP_PROJECT_ID=\n')
        self.assertFalse(report['ready_configuration'])
        self.assertIn('GCP_PROJECT_ID', report['missing_keys'])
    def test_secret_in_file_is_rejected_without_leaking_value(self):
        secret = 'sensitive-unit-test-value'
        report = self.assess('N8N_AUTH_TOKEN=' + secret)
        self.assertTrue(report['issues'])
        self.assertNotIn(secret, json.dumps(report))
    def test_environment_overrides_file_and_can_complete_configuration(self):
        env = {'GCP_PROJECT_ID':'assigned-project', 'VERTEX_MODEL':'assigned-model',
               'N8N_WEBHOOK_URL':'https://automation.company.invalid/webhook/mail',
               'BIGQUERY_PROJECT_ID':'academy-project', 'BIGQUERY_DATASET':'academy',
               'BIGQUERY_TABLE':'sample', 'N8N_AUTH_TOKEN':'env-unit-test-value'}
        report = self.assess('GCP_PROJECT_ID=\n', env)
        self.assertTrue(report['ready_configuration'])
        self.assertEqual('configured', report['integrations']['email'])
        self.assertEqual('configured', report['integrations']['test_data'])
        self.assertNotIn('env-unit-test-value', json.dumps(report))
        env['VIBE_LOCAL'] = '1'
        self.assertFalse(self.assess('', env)['ready_configuration'])
    def test_core_only_deploys_with_integrations_off(self):
        report = self.assess('GCP_PROJECT_ID=p\nVERTEX_MODEL=m\n')
        self.assertTrue(report['ready_configuration'])
        self.assertEqual('not-configured', report['integrations']['email'])
        self.assertEqual('not-configured', report['integrations']['test_data'])
    def test_partial_integration_is_reported_but_not_blocking(self):
        report = self.assess('GCP_PROJECT_ID=p\nVERTEX_MODEL=m\nN8N_WEBHOOK_URL=https://automation.company.invalid/webhook/mail\n')
        self.assertTrue(report['ready_configuration'])
        self.assertEqual('partial', report['integrations']['email'])
    def test_basic_auth_and_test_webhook_are_rejected(self):
        report = self.assess('N8N_HEADER_NAME=Authorization\nN8N_WEBHOOK_URL=https://automation.company.invalid/webhook-test/mail\n')
        self.assertEqual(2, len(report['issues']))
    def test_embedded_url_credentials_are_never_echoed(self):
        report = self.assess('N8N_WEBHOOK_URL=https://user:secret-test-value@host.invalid/webhook/mail\n')
        self.assertTrue(report['issues'])
        self.assertNotIn('secret-test-value', json.dumps(report))

if __name__ == '__main__':
    unittest.main()
