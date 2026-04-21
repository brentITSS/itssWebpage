import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  DocumentClassificationSuggestionDto,
  DocumentExtractionFieldDto,
  DocumentExtractionPreviewResponse,
  DocumentExtractionTemplateDto,
  DocumentLabelSetDto,
  DocumentSummarisationPreviewResponse,
  DocumentSummarisationTemplateDto,
  documentHubService,
} from '../../../services/documentHubService';

type HubTab = 'classification' | 'summarisation' | 'extraction';

const tabClass = (active: boolean) =>
  [
    'rounded-lg border px-3 py-2 text-sm font-medium transition',
    active
      ? 'border-slate-900 bg-slate-900 text-white'
      : 'border-slate-300 bg-white text-slate-700 hover:border-slate-400',
  ].join(' ');

const DocumentHub: React.FC = () => {
  const [activeTab, setActiveTab] = useState<HubTab>('classification');
  const [loading, setLoading] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);

  const [labelSetName, setLabelSetName] = useState('');
  const [labelSetDescription, setLabelSetDescription] = useState('');
  const [classificationPrompt, setClassificationPrompt] = useState(
    'Classify this document into the closest label based on both text and visual layout.'
  );
  const [classificationFiles, setClassificationFiles] = useState<File[]>([]);
  const [classificationSuggestions, setClassificationSuggestions] = useState<DocumentClassificationSuggestionDto[]>([]);

  const [summarisationName, setSummarisationName] = useState('');
  const [summarisationDescription, setSummarisationDescription] = useState('');
  const [summarisationPrompt, setSummarisationPrompt] = useState(
    'Summarise this document using headings, key decisions, and next actions.'
  );
  const [summarisationTestFile, setSummarisationTestFile] = useState<File | null>(null);
  const [summarisationPreview, setSummarisationPreview] = useState<DocumentSummarisationPreviewResponse | null>(null);

  const [extractionTemplateName, setExtractionTemplateName] = useState('');
  const [extractionTemplateDescription, setExtractionTemplateDescription] = useState('');
  const [extractionTestFile, setExtractionTestFile] = useState<File | null>(null);
  const [extractionPreview, setExtractionPreview] = useState<DocumentExtractionPreviewResponse | null>(null);
  const [showExtractionTrainer, setShowExtractionTrainer] = useState(false);
  const [trainerFieldName, setTrainerFieldName] = useState('');
  const [trainerSelectedValue, setTrainerSelectedValue] = useState('');
  const [stagedExtractionFields, setStagedExtractionFields] = useState<
    Array<Pick<DocumentExtractionFieldDto, 'fieldName' | 'exampleValue'>>
  >([]);

  const [labelSets, setLabelSets] = useState<DocumentLabelSetDto[]>([]);
  const [summarisationTemplates, setSummarisationTemplates] = useState<DocumentSummarisationTemplateDto[]>([]);
  const [extractionTemplates, setExtractionTemplates] = useState<DocumentExtractionTemplateDto[]>([]);

  const getFriendlyError = useCallback((error: unknown): string => {
    const raw = error instanceof Error ? error.message : 'Unexpected error.';
    const normalized = raw.toLowerCase();
    if (normalized.includes('404') || normalized.includes('not found')) {
      return 'Document Hub API is not available on the current backend deployment. Deploy the backend feature branch to test save and preview actions.';
    }
    return raw;
  }, []);

  const setupHint = useMemo(() => {
    if (activeTab === 'classification') {
      return 'Upload one or more sample documents, then review auto-generated labels and prompts before saving the label set.';
    }

    if (activeTab === 'summarisation') {
      return 'Define a reusable summarisation goal and prompt. This can be selected when users upload future documents.';
    }

    return 'Upload a sample document, highlight values, and confirm field names. Corrections will improve future extraction runs.';
  }, [activeTab]);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [loadedLabelSets, loadedSummaries, loadedExtractors] = await Promise.all([
        documentHubService.getLabelSets(),
        documentHubService.getSummarisationTemplates(),
        documentHubService.getExtractionTemplates(),
      ]);
      setLabelSets(loadedLabelSets);
      setSummarisationTemplates(loadedSummaries);
      setExtractionTemplates(loadedExtractors);
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  }, [getFriendlyError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleSaveLabelSet = async () => {
    if (!labelSetName.trim()) {
      setFeedback('Label set name is required.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      const created = await documentHubService.createLabelSet({
        labelSetName: labelSetName.trim(),
        labelSetDescription: labelSetDescription.trim() || undefined,
      });

      const labelsToSave =
        classificationSuggestions.length > 0
          ? classificationSuggestions
          : [
              {
                fileName: 'Manual seed',
                suggestedLabel:
                  labelSetName
                    .trim()
                    .split(/\s+/)
                    .slice(0, 2)
                    .join(' ') || 'General Doc',
                suggestedDescription: '',
                suggestedPrompt: classificationPrompt.trim(),
                textPreview: '',
              },
            ];

      for (const suggestion of labelsToSave) {
        await documentHubService.createClassificationLabel(created.documentLabelSetId, {
          classificationLabel: suggestion.suggestedLabel,
          classificationDescription: suggestion.suggestedDescription,
          classificationPrompt: suggestion.suggestedPrompt,
          seedDocumentName: suggestion.fileName,
          isAutoGenerated: true,
        });
      }

      setLabelSetName('');
      setLabelSetDescription('');
      setClassificationFiles([]);
      setClassificationSuggestions([]);
      setFeedback('Classification label set saved.');
      await loadData();
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleSaveSummarisationTemplate = async () => {
    if (!summarisationName.trim() || !summarisationPrompt.trim()) {
      setFeedback('Summarisation name and prompt are required.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      await documentHubService.createSummarisationTemplate({
        summarisationName: summarisationName.trim(),
        summarisationDescription: summarisationDescription.trim() || undefined,
        summarisationPrompt: summarisationPrompt.trim(),
      });

      setSummarisationName('');
      setSummarisationDescription('');
      setSummarisationTestFile(null);
      setFeedback('Summarisation template saved.');
      await loadData();
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleSaveExtractionTemplate = async () => {
    if (!extractionTemplateName.trim()) {
      setFeedback('Extraction template name is required.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      const template = await documentHubService.createExtractionTemplate({
        extractionTemplateName: extractionTemplateName.trim(),
        extractionTemplateDescription: extractionTemplateDescription.trim() || undefined,
      });

      for (const field of stagedExtractionFields) {
        await documentHubService.createExtractionField(template.documentExtractionTemplateId, {
          fieldName: field.fieldName.trim(),
          exampleValue: field.exampleValue?.trim() || undefined,
        });
      }

      setExtractionTemplateName('');
      setExtractionTemplateDescription('');
      setExtractionTestFile(null);
      setExtractionPreview(null);
      setStagedExtractionFields([]);
      setShowExtractionTrainer(false);
      setTrainerFieldName('');
      setTrainerSelectedValue('');
      setFeedback('Extraction template saved.');
      await loadData();
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleGenerateClassificationSuggestions = async () => {
    if (classificationFiles.length === 0) {
      setFeedback('Upload one or more PDF files first.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      const suggestions = await documentHubService.suggestClassificationLabels(classificationFiles);
      setClassificationSuggestions(suggestions);
      setFeedback(`Generated ${suggestions.length} label suggestions.`);
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleRunSummarisationPreview = async () => {
    if (!summarisationTestFile) {
      setFeedback('Upload a PDF to test summarisation.');
      return;
    }

    if (!summarisationPrompt.trim()) {
      setFeedback('Summarisation prompt is required.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      const preview = await documentHubService.previewSummarisation(summarisationTestFile, summarisationPrompt.trim());
      setSummarisationPreview(preview);
      setFeedback('Summarisation preview generated.');
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handlePrepareExtractionPreview = async () => {
    if (!extractionTestFile) {
      setFeedback('Upload a PDF to start extraction training.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      const preview = await documentHubService.previewExtraction(extractionTestFile);
      setExtractionPreview(preview);
      setShowExtractionTrainer(true);
      setFeedback('Extraction preview ready. Select values and add fields.');
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const captureSelectedText = () => {
    const selected = window.getSelection()?.toString().trim() || '';
    if (!selected) {
      setFeedback('Highlight text in the extraction modal first.');
      return;
    }
    setTrainerSelectedValue(selected);
    if (!trainerFieldName) {
      const autoName = selected
        .split(':')[0]
        .replace(/[^a-zA-Z0-9 ]/g, '')
        .trim()
        .toLowerCase()
        .slice(0, 40);
      setTrainerFieldName(autoName || 'extracted field');
    }
  };

  const addStagedField = () => {
    if (!trainerFieldName.trim() || !trainerSelectedValue.trim()) {
      setFeedback('Field name and selected value are required.');
      return;
    }

    setStagedExtractionFields((prev) => [
      ...prev,
      { fieldName: trainerFieldName.trim(), exampleValue: trainerSelectedValue.trim() },
    ]);
    setTrainerFieldName('');
    setTrainerSelectedValue('');
  };

  return (
    <section className="space-y-6">
      <div className="rounded-xl border border-slate-200 bg-white p-5">
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Document Hub</h2>
        <p className="mt-1 text-sm text-slate-500">
          Configure global document classification, text summarisation, and entity extraction workflows.
        </p>
        <p className="mt-3 rounded-lg border border-indigo-100 bg-indigo-50 px-3 py-2 text-xs text-indigo-700">
          {setupHint}
        </p>
        {feedback && (
          <p className="mt-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700">{feedback}</p>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <button type="button" className={tabClass(activeTab === 'classification')} onClick={() => setActiveTab('classification')}>
          Classification
        </button>
        <button type="button" className={tabClass(activeTab === 'summarisation')} onClick={() => setActiveTab('summarisation')}>
          Text Summarisation
        </button>
        <button type="button" className={tabClass(activeTab === 'extraction')} onClick={() => setActiveTab('extraction')}>
          Entity Extraction
        </button>
      </div>

      {activeTab === 'classification' && (
        <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
          <div className="rounded-xl border border-slate-200 bg-white p-5">
            <h3 className="text-lg font-semibold text-slate-900">Classification Label Set</h3>
            <div className="mt-4 space-y-4">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Label Set Name</span>
                <input
                  value={labelSetName}
                  onChange={(event) => setLabelSetName(event.target.value)}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                  placeholder="e.g. Tenancy Documents"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Label Set Description</span>
                <textarea
                  value={labelSetDescription}
                  onChange={(event) => setLabelSetDescription(event.target.value)}
                  className="mt-1 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                  placeholder="Describe what document family this label set should classify."
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Classification Prompt Template</span>
                <textarea
                  value={classificationPrompt}
                  onChange={(event) => setClassificationPrompt(event.target.value)}
                  className="mt-1 min-h-28 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                />
              </label>
            </div>
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-5">
            <h3 className="text-lg font-semibold text-slate-900">Seed Documents</h3>
            <p className="mt-1 text-sm text-slate-500">
              Upload one or more sample PDFs to auto-generate 2-word labels and editable prompts.
            </p>
            <div className="mt-4 rounded-lg border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
              <input
                type="file"
                multiple
                accept=".pdf"
                onChange={(event) => setClassificationFiles(Array.from(event.target.files ?? []))}
                className="mx-auto block text-sm text-slate-700"
              />
              <p className="mt-2 text-xs">
                {classificationFiles.length === 0
                  ? 'No files selected.'
                  : `${classificationFiles.length} file(s) selected.`}
              </p>
            </div>
            <button
              type="button"
              onClick={handleGenerateClassificationSuggestions}
              disabled={loading}
              className="mt-4 mr-2 inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400"
            >
              {loading ? 'Generating...' : 'Generate Labels from Uploads'}
            </button>
            <button
              type="button"
              onClick={handleSaveLabelSet}
              disabled={loading}
              className="mt-4 inline-flex items-center rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700"
            >
              {loading ? 'Saving...' : 'Save Label Set'}
            </button>
            <div className="mt-4 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
              <p className="font-semibold text-slate-700">Saved Label Sets</p>
              {labelSets.length === 0 ? (
                <p className="mt-1">No label sets saved yet.</p>
              ) : (
                <ul className="mt-2 space-y-1">
                  {labelSets.slice(0, 5).map((item) => (
                    <li key={item.documentLabelSetId}>
                      {item.labelSetName} ({item.labels.length} labels)
                    </li>
                  ))}
                </ul>
              )}
            </div>
            {classificationSuggestions.length > 0 && (
              <div className="mt-4 space-y-2 rounded-lg border border-slate-200 bg-white p-3 text-xs">
                <p className="font-semibold text-slate-700">Generated Label Suggestions</p>
                {classificationSuggestions.map((item, idx) => (
                  <div key={`${item.fileName}-${idx}`} className="rounded border border-slate-200 p-2">
                    <p className="font-medium text-slate-800">{item.fileName}</p>
                    <input
                      value={item.suggestedLabel}
                      onChange={(event) =>
                        setClassificationSuggestions((prev) =>
                          prev.map((x, i) => (i === idx ? { ...x, suggestedLabel: event.target.value } : x))
                        )
                      }
                      className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                    />
                    <textarea
                      value={item.suggestedDescription}
                      onChange={(event) =>
                        setClassificationSuggestions((prev) =>
                          prev.map((x, i) => (i === idx ? { ...x, suggestedDescription: event.target.value } : x))
                        )
                      }
                      className="mt-1 min-h-16 w-full rounded border border-slate-300 px-2 py-1"
                    />
                    <textarea
                      value={item.suggestedPrompt}
                      onChange={(event) =>
                        setClassificationSuggestions((prev) =>
                          prev.map((x, i) => (i === idx ? { ...x, suggestedPrompt: event.target.value } : x))
                        )
                      }
                      className="mt-1 min-h-16 w-full rounded border border-slate-300 px-2 py-1"
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'summarisation' && (
        <div className="rounded-xl border border-slate-200 bg-white p-5">
          <h3 className="text-lg font-semibold text-slate-900">Text Summarisation Template</h3>
          <div className="mt-4 grid gap-4">
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Text Summarisation Name</span>
              <input
                value={summarisationName}
                onChange={(event) => setSummarisationName(event.target.value)}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                placeholder="e.g. Compliance Brief"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Text Summarisation Description</span>
              <textarea
                value={summarisationDescription}
                onChange={(event) => setSummarisationDescription(event.target.value)}
                className="mt-1 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                placeholder="Describe the summary goal and expected tone."
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Text Summarisation Prompt</span>
              <textarea
                value={summarisationPrompt}
                onChange={(event) => setSummarisationPrompt(event.target.value)}
                className="mt-1 min-h-32 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Test Document Upload (temporary)</span>
              <input
                type="file"
                accept=".pdf"
                onChange={(event) => setSummarisationTestFile(event.target.files?.[0] ?? null)}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
            </label>
          </div>
          <button
            type="button"
            onClick={handleRunSummarisationPreview}
            disabled={loading}
            className="mt-4 mr-2 inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400"
          >
            {loading ? 'Testing...' : 'Test Prompt Against Document'}
          </button>
          <button
            type="button"
            onClick={handleSaveSummarisationTemplate}
            disabled={loading}
            className="mt-4 inline-flex items-center rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700"
          >
            {loading ? 'Saving...' : 'Save Summarisation Template'}
          </button>
          <div className="mt-4 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
            <p className="font-semibold text-slate-700">Saved Summarisation Templates</p>
            {summarisationTemplates.length === 0 ? (
              <p className="mt-1">No templates saved yet.</p>
            ) : (
              <ul className="mt-2 space-y-1">
                {summarisationTemplates.slice(0, 5).map((item) => (
                  <li key={item.documentSummarisationTemplateId}>{item.summarisationName}</li>
                ))}
              </ul>
            )}
          </div>
          {summarisationPreview && (
            <div className="mt-4 rounded-lg border border-indigo-100 bg-indigo-50 p-3 text-xs text-indigo-900">
              <p className="font-semibold">Summary Preview ({summarisationPreview.fileName})</p>
              <p className="mt-2 whitespace-pre-wrap">{summarisationPreview.summary}</p>
            </div>
          )}
        </div>
      )}

      {activeTab === 'extraction' && (
        <div className="grid gap-6 xl:grid-cols-[1.3fr_1fr]">
          <div className="rounded-xl border border-slate-200 bg-white p-5">
            <h3 className="text-lg font-semibold text-slate-900">Entity Extraction Template</h3>
            <div className="mt-4 space-y-4">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Template Name</span>
                <input
                  value={extractionTemplateName}
                  onChange={(event) => setExtractionTemplateName(event.target.value)}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                  placeholder="e.g. Account Statement Fields"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Template Description</span>
                <textarea
                  value={extractionTemplateDescription}
                  onChange={(event) => setExtractionTemplateDescription(event.target.value)}
                  className="mt-1 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                  placeholder="Describe when this extraction should be used."
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Upload Training Document (temporary)</span>
                <input
                  type="file"
                  accept=".pdf"
                  onChange={(event) => setExtractionTestFile(event.target.files?.[0] ?? null)}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </label>
              <button
                type="button"
                onClick={handlePrepareExtractionPreview}
                disabled={loading}
                className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400"
              >
                {loading ? 'Preparing...' : 'Open Extraction Trainer'}
              </button>
            </div>
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-5">
            <h3 className="text-lg font-semibold text-slate-900">Captured Fields</h3>
            <div className="mt-4 space-y-4">
              {stagedExtractionFields.length === 0 ? (
                <p className="text-sm text-slate-500">No fields captured yet. Use Open Extraction Trainer.</p>
              ) : (
                stagedExtractionFields.map((field, index) => (
                  <div key={`${field.fieldName}-${index}`} className="rounded-lg border border-slate-200 p-2 text-sm">
                    <p className="font-medium text-slate-800">{field.fieldName}</p>
                    <p className="text-slate-600">{field.exampleValue}</p>
                  </div>
                ))
              )}
              <p className="rounded-lg border border-emerald-100 bg-emerald-50 px-3 py-2 text-xs text-emerald-700">
                Future correction feedback will be saved and reused to improve extraction quality over time.
              </p>
            </div>
            <button
              type="button"
              onClick={handleSaveExtractionTemplate}
              disabled={loading}
              className="mt-4 inline-flex items-center rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700"
            >
              {loading ? 'Saving...' : 'Complete Extraction Template'}
            </button>
            <div className="mt-4 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
              <p className="font-semibold text-slate-700">Saved Extraction Templates</p>
              {extractionTemplates.length === 0 ? (
                <p className="mt-1">No templates saved yet.</p>
              ) : (
                <ul className="mt-2 space-y-1">
                  {extractionTemplates.slice(0, 5).map((item) => (
                    <li key={item.documentExtractionTemplateId}>
                      {item.extractionTemplateName} ({item.fields.length} fields)
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </div>
      )}

      {showExtractionTrainer && extractionPreview && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm px-4 py-6">
          <div className="mx-auto max-w-6xl rounded-xl border border-slate-200 bg-white p-5">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold text-slate-900">Entity Extraction Trainer</h3>
                <p className="text-xs text-slate-500">
                  Highlight values in extracted text, assign field names, then add to template.
                </p>
              </div>
              <button
                type="button"
                onClick={() => setShowExtractionTrainer(false)}
                className="rounded-md border border-slate-300 px-3 py-1 text-sm text-slate-700"
              >
                Close
              </button>
            </div>
            <div className="grid gap-4 lg:grid-cols-[1.5fr_1fr]">
              <div className="max-h-[60vh] overflow-y-auto rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm text-slate-800">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">{extractionPreview.fileName}</p>
                <pre className="whitespace-pre-wrap select-text">{extractionPreview.extractedText || extractionPreview.textPreview}</pre>
              </div>
              <div className="space-y-3 rounded-lg border border-slate-200 p-3">
                <button
                  type="button"
                  onClick={captureSelectedText}
                  className="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400"
                >
                  Capture Selected Text
                </button>
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">Field Name</span>
                  <input
                    value={trainerFieldName}
                    onChange={(event) => setTrainerFieldName(event.target.value)}
                    className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                </label>
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">Example Value</span>
                  <textarea
                    value={trainerSelectedValue}
                    onChange={(event) => setTrainerSelectedValue(event.target.value)}
                    className="mt-1 min-h-24 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                </label>
                <button
                  type="button"
                  onClick={addStagedField}
                  className="inline-flex items-center rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700"
                >
                  Add Field
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  );
};

export default DocumentHub;
