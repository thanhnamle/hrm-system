// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

/**
 * Spec files never import each other: the test builder can then run one spec's describe() inside
 * the importing file, and Vitest intermittently reports "No test suite found". Shared fixtures go
 * in src/testing/. (Listed in both blocks below: a later block's rule replaces, not merges.)
 */
const NO_SPEC_IMPORTS = {
  group: ['**/*.spec', '**/*.spec.ts'],
  message: 'Do not import from a spec file; put shared test fixtures in src/testing/.',
};

module.exports = defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      'no-restricted-imports': ['error', { patterns: [NO_SPEC_IMPORTS] }],
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
    },
  },
  {
    // Layering (docs/architecture.md): core and shared are used by features, never the other way round.
    files: ['src/app/core/**/*.ts', 'src/app/shared/**/*.ts', 'src/app/layout/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            { group: ['**/features/**'], message: 'core, shared and layout must not import from features.' },
            NO_SPEC_IMPORTS,
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
