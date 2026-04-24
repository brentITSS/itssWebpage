import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Document, Page, pdfjs } from 'react-pdf';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import 'react-pdf/dist/Page/TextLayer.css';
import {
  DocumentClassificationSuggestionDto,
  DocumentExtractionPreviewResponse,
  DocumentExtractionSuggestedFieldDto,
  DocumentExtractionTemplateDto,
  DocumentLabelSetDto,
  DocumentSummarisationPreviewResponse,
  DocumentSummarisationTemplateDto,
  documentHubService,
} from '../../../services/documentHubService';

pdfjs.GlobalWorkerOptions.workerSrc = `https://unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`;

type HubTab = 'classification' | 'summarisation' | 'extraction';

type TrainerField = {
  id: string;
  fieldName: string;
  exampleValue: string;
  boundingBoxJson?: string;
  pageNumber?: number;
};

type PersistentHighlight = {
  id: string;
  color: string;
  selectedText: string;
  rects: Array<{ left: number; top: number; width: number; height: number }>;
};

const HIGHLIGHT_COLORS = ['#fde68a', '#bfdbfe', '#fecdd3', '#bbf7d0', '#ddd6fe', '#fdba74'];
type MobileTrainerView = 'pdf' | 'fields';
type DragPoint = { x: number; y: number };

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
  const [summarisationFeedback, setSummarisationFeedback] = useState<string | null>(null);
  const [editingSummarisationTemplateId, setEditingSummarisationTemplateId] = useState<number | null>(null);

  const [extractionTemplateName, setExtractionTemplateName] = useState('');
  const [extractionTemplateDescription, setExtractionTemplateDescription] = useState('');
  const [extractionTestFile, setExtractionTestFile] = useState<File | null>(null);
  const [extractionPreview, setExtractionPreview] = useState<DocumentExtractionPreviewResponse | null>(null);
  const [showExtractionTrainer, setShowExtractionTrainer] = useState(false);
  const [stagedExtractionFields, setStagedExtractionFields] = useState<TrainerField[]>([]);
  const [suggestedExtractionFields, setSuggestedExtractionFields] = useState<DocumentExtractionSuggestedFieldDto[]>([]);
  const [trainerHighlights, setTrainerHighlights] = useState<PersistentHighlight[]>([]);
  const [pdfPageCount, setPdfPageCount] = useState(0);
  const [pdfScale, setPdfScale] = useState(1.55);
  const [pdfLoadError, setPdfLoadError] = useState<string | null>(null);
  const [selectionLoading, setSelectionLoading] = useState(false);
  const [mobileTrainerView, setMobileTrainerView] = useState<MobileTrainerView>('pdf');
  const [dragSelectionStart, setDragSelectionStart] = useState<DragPoint | null>(null);
  const [dragSelectionCurrent, setDragSelectionCurrent] = useState<DragPoint | null>(null);
  const pdfSelectionContainerRef = useRef<HTMLDivElement | null>(null);

  const [labelSets, setLabelSets] = useState<DocumentLabelSetDto[]>([]);
  const [summarisationTemplates, setSummarisationTemplates] = useState<DocumentSummarisationTemplateDto[]>([]);
  const [extractionTemplates, setExtractionTemplates] = useState<DocumentExtractionTemplateDto[]>([]);
  const [processingMailboxUser, setProcessingMailboxUser] = useState('');
  const [processingMaxEmails, setProcessingMaxEmails] = useState(20);
  const [emailProcessingResult, setEmailProcessingResult] = useState<Record<string, unknown> | string | null>(null);

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
    setSummarisationFeedback(null);
    try {
      if (editingSummarisationTemplateId) {
        await documentHubService.updateSummarisationTemplate(editingSummarisationTemplateId, {
          summarisationName: summarisationName.trim(),
          summarisationDescription: summarisationDescription.trim() || undefined,
          summarisationPrompt: summarisationPrompt.trim(),
        });
      } else {
        await documentHubService.createSummarisationTemplate({
          summarisationName: summarisationName.trim(),
          summarisationDescription: summarisationDescription.trim() || undefined,
          summarisationPrompt: summarisationPrompt.trim(),
        });
      }

      setSummarisationName('');
      setSummarisationDescription('');
      setSummarisationTestFile(null);
      setEditingSummarisationTemplateId(null);
      setFeedback(editingSummarisationTemplateId ? 'Summarisation template updated.' : 'Summarisation template saved.');
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
          exampleValue: field.exampleValue.trim() || undefined,
          boundingBoxJson: field.boundingBoxJson,
          pageNumber: field.pageNumber,
        });
      }

      setExtractionTemplateName('');
      setExtractionTemplateDescription('');
      setExtractionTestFile(null);
      setExtractionPreview(null);
      setStagedExtractionFields([]);
      setSuggestedExtractionFields([]);
      setTrainerHighlights([]);
      setPdfPageCount(0);
      setPdfScale(1.55);
      setShowExtractionTrainer(false);
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
      setSummarisationFeedback('Upload a PDF to test summarisation.');
      return;
    }

    if (!summarisationPrompt.trim()) {
      setSummarisationFeedback('Summarisation prompt is required.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    setSummarisationFeedback(null);
    try {
      const preview = await documentHubService.previewSummarisation(summarisationTestFile, summarisationPrompt.trim());
      setSummarisationPreview(preview);
      setSummarisationFeedback(`Preview generated for ${preview.fileName}.`);
    } catch (error) {
      setSummarisationFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleEditSummarisationTemplate = (template: DocumentSummarisationTemplateDto) => {
    setEditingSummarisationTemplateId(template.documentSummarisationTemplateId);
    setSummarisationName(template.summarisationName);
    setSummarisationDescription(template.summarisationDescription ?? '');
    setSummarisationPrompt(template.summarisationPrompt);
    setSummarisationFeedback(`Editing template "${template.summarisationName}".`);
  };

  const handleCancelEditSummarisation = () => {
    setEditingSummarisationTemplateId(null);
    setSummarisationName('');
    setSummarisationDescription('');
    setSummarisationPrompt('Summarise this document using headings, key decisions, and next actions.');
    setSummarisationFeedback('Edit cancelled.');
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
      setSuggestedExtractionFields(preview.suggestedFields ?? []);
      setTrainerHighlights([]);
      setPdfPageCount(0);
      setPdfScale(1.55);
      setPdfLoadError(null);
      setMobileTrainerView('pdf');
      setShowExtractionTrainer(true);
      setFeedback('Extraction preview ready. Highlight text directly on the PDF to build fields.');
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const appendTrainerFields = (
    fields: DocumentExtractionSuggestedFieldDto[],
    metadata?: Pick<TrainerField, 'boundingBoxJson' | 'pageNumber'>
  ) => {
    setStagedExtractionFields((prev) => {
      const merged = [...prev];
      for (const field of fields) {
        const fieldName = field.fieldName.trim();
        const exampleValue = field.exampleValue.trim();
        if (!fieldName || !exampleValue) {
          continue;
        }

        const alreadyExists = merged.some(
          (item) =>
            item.fieldName.trim().toLowerCase() === fieldName.toLowerCase() &&
            item.exampleValue.trim().toLowerCase() === exampleValue.toLowerCase()
        );
        if (alreadyExists) {
          continue;
        }

        merged.push({
          id: `field-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
          fieldName,
          exampleValue,
          boundingBoxJson: metadata?.boundingBoxJson,
          pageNumber: metadata?.pageNumber,
        });
      }

      return merged;
    });
  };

  const runSelectionCapture = async (
    selected: string,
    rects: Array<{ left: number; top: number; width: number; height: number }>
  ) => {
    if (!selected) {
      setFeedback('Select an area containing PDF text first.');
      return;
    }

    setTrainerHighlights((prev) => [
      ...prev,
      {
        id: `hl-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
        color: HIGHLIGHT_COLORS[prev.length % HIGHLIGHT_COLORS.length],
        selectedText: selected,
        rects,
      },
    ]);

    setSelectionLoading(true);
    try {
      const aiFields = await documentHubService.suggestExtractionFromSelection({
        selectedText: selected,
        extractedText: extractionPreview?.extractedText ?? '',
      });
      const metadata = { boundingBoxJson: JSON.stringify(rects) };
      if (aiFields.length > 0) {
        appendTrainerFields(aiFields, metadata);
      } else {
        appendTrainerFields([{ fieldName: 'extracted_field', exampleValue: selected }], metadata);
      }
    } catch (error) {
      setFeedback(getFriendlyError(error));
      appendTrainerFields([{ fieldName: 'extracted_field', exampleValue: selected }], {
        boundingBoxJson: JSON.stringify(rects),
      });
    } finally {
      setSelectionLoading(false);
    }
  };

  const captureSelectedText = async () => {
    const selection = window.getSelection();
    const selected = selection?.toString().trim() || '';
    if (!selected) {
      setFeedback('Drag over the PDF to select text.');
      return;
    }

    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    const container = pdfSelectionContainerRef.current;
    if (!range || !container || !container.contains(range.commonAncestorContainer)) {
      setFeedback('Selection must be inside the PDF preview area.');
      return;
    }

    const containerRect = container.getBoundingClientRect();
    const rects = Array.from(range.getClientRects())
      .filter((rect) => rect.width > 1 && rect.height > 1)
      .map((rect) => ({
        left: rect.left - containerRect.left + container.scrollLeft,
        top: rect.top - containerRect.top + container.scrollTop,
        width: rect.width,
        height: rect.height,
      }));

    selection?.removeAllRanges();
    if (rects.length === 0) {
      setFeedback('Could not read the selected area. Try dragging a larger region.');
      return;
    }

    await runSelectionCapture(selected, rects);
  };

  const getPointInSelectionContainer = (event: React.MouseEvent<HTMLDivElement>): DragPoint | null => {
    const container = pdfSelectionContainerRef.current;
    if (!container) {
      return null;
    }

    const bounds = container.getBoundingClientRect();
    return {
      x: event.clientX - bounds.left + container.scrollLeft,
      y: event.clientY - bounds.top + container.scrollTop,
    };
  };

  const getNormalizedRect = (start: DragPoint, end: DragPoint) => {
    const left = Math.min(start.x, end.x);
    const top = Math.min(start.y, end.y);
    const width = Math.abs(start.x - end.x);
    const height = Math.abs(start.y - end.y);
    return { left, top, width, height };
  };

  const collectTextFromRect = (targetRect: { left: number; top: number; width: number; height: number }) => {
    const container = pdfSelectionContainerRef.current;
    if (!container) {
      return '';
    }

    const containerBounds = container.getBoundingClientRect();
    const spans = Array.from(container.querySelectorAll('.react-pdf__Page__textContent span'));
    const collected: string[] = [];

    for (const span of spans) {
      const text = (span.textContent || '').trim();
      if (!text) {
        continue;
      }

      const spanRect = span.getBoundingClientRect();
      const relativeRect = {
        left: spanRect.left - containerBounds.left + container.scrollLeft,
        top: spanRect.top - containerBounds.top + container.scrollTop,
        width: spanRect.width,
        height: spanRect.height,
      };

      const intersects =
        relativeRect.left < targetRect.left + targetRect.width &&
        relativeRect.left + relativeRect.width > targetRect.left &&
        relativeRect.top < targetRect.top + targetRect.height &&
        relativeRect.top + relativeRect.height > targetRect.top;

      if (intersects) {
        collected.push(text);
      }
    }

    return collected.join(' ').replace(/\s+/g, ' ').trim();
  };

  const handleDragSelectionStart = (event: React.MouseEvent<HTMLDivElement>) => {
    if (event.button !== 0 || selectionLoading) {
      return;
    }

    const point = getPointInSelectionContainer(event);
    if (!point) {
      return;
    }

    setDragSelectionStart(point);
    setDragSelectionCurrent(point);
  };

  const handleDragSelectionMove = (event: React.MouseEvent<HTMLDivElement>) => {
    if (!dragSelectionStart) {
      return;
    }

    const point = getPointInSelectionContainer(event);
    if (!point) {
      return;
    }

    setDragSelectionCurrent(point);
  };

  const handleDragSelectionEnd = async (event: React.MouseEvent<HTMLDivElement>) => {
    if (!dragSelectionStart) {
      return;
    }

    const point = getPointInSelectionContainer(event);
    const endPoint = point ?? dragSelectionCurrent ?? dragSelectionStart;
    const normalized = getNormalizedRect(dragSelectionStart, endPoint);

    setDragSelectionStart(null);
    setDragSelectionCurrent(null);

    if (normalized.width < 8 || normalized.height < 8) {
      return;
    }

    const selected = collectTextFromRect(normalized);
    if (!selected) {
      setFeedback('No selectable PDF text found in that area. Try a larger drag box.');
      return;
    }

    await runSelectionCapture(selected, [normalized]);
  };

  const dragPreviewRect =
    dragSelectionStart && dragSelectionCurrent ? getNormalizedRect(dragSelectionStart, dragSelectionCurrent) : null;

  const updateStagedField = (id: string, patch: Partial<Pick<TrainerField, 'fieldName' | 'exampleValue'>>) => {
    setStagedExtractionFields((prev) => prev.map((field) => (field.id === id ? { ...field, ...patch } : field)));
  };

  const removeStagedField = (id: string) => {
    setStagedExtractionFields((prev) => prev.filter((field) => field.id !== id));
  };

  const addSuggestedField = (field: DocumentExtractionSuggestedFieldDto) => {
    appendTrainerFields([field]);
  };

  const handleTriggerEmailProcessing = async () => {
    setLoading(true);
    setFeedback(null);
    try {
      const response = await documentHubService.triggerPropertyHubEmailProcessing({
        mailboxUser: processingMailboxUser.trim() || undefined,
        maxEmails: Number.isFinite(processingMaxEmails) ? processingMaxEmails : undefined,
      });
      const result = response.processingResult;
      setEmailProcessingResult(
        typeof result === 'string' || (typeof result === 'object' && result !== null)
          ? (result as Record<string, unknown> | string)
          : null
      );
      setFeedback(response.message || 'Email processing completed.');
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-xl border border-slate-200 bg-white p-5">
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Document Hub</h2>
        <p className="mt-1 text-sm text-slate-500">
          Configure global document classification, text summarisation, and entity extraction workflows.
        </p>
        <div className="mt-3 rounded-lg border border-indigo-100 bg-indigo-50 px-3 py-2 text-xs text-indigo-700">{setupHint}</div>
        {feedback && (
          <p className="mt-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700">{feedback}</p>
        )}
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-5">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-600">Email Trigger (Inbox/Property Hub)</p>
        <div className="mt-2 grid gap-2 md:grid-cols-[1fr_130px_auto]">
          <input
            value={processingMailboxUser}
            onChange={(event) => setProcessingMailboxUser(event.target.value)}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
            placeholder="Mailbox user override (optional)"
          />
          <input
            type="number"
            min={1}
            max={100}
            value={processingMaxEmails}
            onChange={(event) => setProcessingMaxEmails(Number(event.target.value || 20))}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
            placeholder="Max emails"
          />
          <button
            type="button"
            onClick={handleTriggerEmailProcessing}
            disabled={loading}
            className="inline-flex items-center justify-center rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {loading ? 'Processing...' : 'Process Property Hub Emails'}
          </button>
        </div>
        {emailProcessingResult !== null && (
          <pre className="mt-2 max-h-48 overflow-auto rounded border border-slate-200 bg-slate-50 p-2 text-[11px] text-slate-700">
            {JSON.stringify(emailProcessingResult, null, 2)}
          </pre>
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
            {loading ? 'Saving...' : editingSummarisationTemplateId ? 'Update Summarisation Template' : 'Save Summarisation Template'}
          </button>
          {editingSummarisationTemplateId && (
            <button
              type="button"
              onClick={handleCancelEditSummarisation}
              disabled={loading}
              className="mt-4 ml-2 inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400"
            >
              Cancel Edit
            </button>
          )}
          {summarisationFeedback && (
            <div className="mt-4 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700">{summarisationFeedback}</div>
          )}
          <div className="mt-4 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
            <p className="font-semibold text-slate-700">Saved Summarisation Templates</p>
            {summarisationTemplates.length === 0 ? (
              <p className="mt-1">No templates saved yet.</p>
            ) : (
              <ul className="mt-2 space-y-2">
                {summarisationTemplates.slice(0, 5).map((item) => (
                  <li key={item.documentSummarisationTemplateId} className="flex items-center justify-between rounded border border-slate-200 bg-white px-2 py-1">
                    <span>{item.summarisationName}</span>
                    <button
                      type="button"
                      onClick={() => handleEditSummarisationTemplate(item)}
                      className="rounded border border-slate-300 px-2 py-0.5 text-[11px] font-medium text-slate-700 hover:border-slate-400"
                    >
                      Edit
                    </button>
                  </li>
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
                stagedExtractionFields.map((field) => (
                  <div key={field.id} className="rounded-lg border border-slate-200 p-2 text-sm">
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
        <div className="fixed inset-0 z-50 bg-slate-900/45 backdrop-blur-sm p-0 sm:p-3 lg:p-6">
          <div className="mx-auto flex h-full w-full max-w-[96vw] flex-col rounded-none border border-slate-200 bg-white p-3 sm:rounded-xl sm:p-4 lg:p-5">
            <div className="mb-3 flex items-center justify-between border-b border-slate-200 pb-3">
              <div>
                <h3 className="text-lg font-semibold text-slate-900">Entity Extraction Trainer</h3>
                <p className="text-xs text-slate-500">
                  Highlight text directly on the PDF preview and let AI generate editable field/value mappings.
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
            <div className="mb-3 flex items-center gap-2 lg:hidden">
              <button
                type="button"
                onClick={() => setMobileTrainerView('pdf')}
                className={`rounded-md px-3 py-1.5 text-xs font-semibold ${
                  mobileTrainerView === 'pdf'
                    ? 'bg-slate-900 text-white'
                    : 'border border-slate-300 bg-white text-slate-700'
                }`}
              >
                PDF
              </button>
              <button
                type="button"
                onClick={() => setMobileTrainerView('fields')}
                className={`rounded-md px-3 py-1.5 text-xs font-semibold ${
                  mobileTrainerView === 'fields'
                    ? 'bg-slate-900 text-white'
                    : 'border border-slate-300 bg-white text-slate-700'
                }`}
              >
                Fields
              </button>
            </div>
            <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[minmax(0,1fr)_380px]">
              <div className={`${mobileTrainerView === 'pdf' ? 'flex' : 'hidden'} min-h-0 flex-col rounded-lg border border-slate-200 bg-slate-50 p-3 lg:flex`}>
                <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Uploaded PDF - highlight directly on document
                  </p>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setPdfScale((prev) => Math.max(1, Number((prev - 0.15).toFixed(2))))}
                      className="rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-700"
                    >
                      Zoom -
                    </button>
                    <button
                      type="button"
                      onClick={() => setPdfScale((prev) => Math.min(2.6, Number((prev + 0.15).toFixed(2))))}
                      className="rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-700"
                    >
                      Zoom +
                    </button>
                    <span className="text-xs text-slate-500">{Math.round(pdfScale * 100)}%</span>
                  </div>
                </div>
                <div
                  ref={pdfSelectionContainerRef}
                  className="relative min-h-0 flex-1 cursor-crosshair overflow-auto rounded border border-slate-300 bg-white p-2"
                  onMouseDown={handleDragSelectionStart}
                  onMouseMove={handleDragSelectionMove}
                  onMouseUp={handleDragSelectionEnd}
                  onMouseLeave={handleDragSelectionEnd}
                >
                  {extractionTestFile ? (
                    <Document
                      file={extractionTestFile}
                      onLoadSuccess={({ numPages }) => {
                        setPdfPageCount(numPages);
                        setPdfLoadError(null);
                      }}
                      onLoadError={(error) => {
                        setPdfLoadError(error?.message || 'Failed to load PDF file.');
                        setPdfPageCount(0);
                      }}
                      className="flex flex-col items-center gap-3"
                    >
                      {Array.from({ length: pdfPageCount }, (_, pageIndex) => (
                        <div key={`page-${pageIndex + 1}`} className="shadow-sm">
                          <Page
                            pageNumber={pageIndex + 1}
                            scale={pdfScale}
                            renderAnnotationLayer
                            renderTextLayer
                          />
                        </div>
                      ))}
                    </Document>
                  ) : (
                    <div className="rounded border border-dashed border-slate-300 bg-white p-4 text-xs text-slate-500">
                      PDF preview is unavailable for this file.
                    </div>
                  )}

                  {pdfLoadError && (
                    <div className="mt-2 rounded border border-rose-200 bg-rose-50 p-2 text-xs text-rose-700">
                      {pdfLoadError}
                    </div>
                  )}

                  {dragPreviewRect && (
                    <div
                      className="pointer-events-none absolute rounded border-2 border-dashed border-indigo-500 bg-indigo-200/25"
                      style={{
                        left: dragPreviewRect.left,
                        top: dragPreviewRect.top,
                        width: dragPreviewRect.width,
                        height: dragPreviewRect.height,
                      }}
                    />
                  )}

                  {trainerHighlights.map((highlight) =>
                    highlight.rects.map((rect, idx) => (
                      <div
                        key={`${highlight.id}-${idx}`}
                        className="pointer-events-none absolute rounded-sm border"
                        style={{
                          left: rect.left,
                          top: rect.top,
                          width: rect.width,
                          height: rect.height,
                          borderColor: highlight.color,
                          backgroundColor: `${highlight.color}55`,
                        }}
                        title={highlight.selectedText}
                      />
                    ))
                  )}
                </div>
              </div>
              <div className={`${mobileTrainerView === 'fields' ? 'flex' : 'hidden'} min-h-0 flex-col space-y-3 rounded-lg border border-slate-200 p-3 lg:flex`}>
                {suggestedExtractionFields.length > 0 && (
                  <div className="rounded-lg border border-indigo-100 bg-indigo-50 p-2">
                    <p className="text-xs font-semibold text-indigo-800">AI Suggested Fields</p>
                    <div className="mt-2 max-h-[30vh] space-y-1 overflow-y-auto pr-1">
                      {suggestedExtractionFields.map((field, idx) => (
                        <div key={`${field.fieldName}-${idx}`} className="rounded border border-indigo-200 bg-white p-2 text-xs">
                          <input
                            value={field.fieldName}
                            onChange={(event) =>
                              setSuggestedExtractionFields((prev) =>
                                prev.map((item, index) =>
                                  index === idx ? { ...item, fieldName: event.target.value } : item
                                )
                              )
                            }
                            className="w-full rounded border border-slate-300 px-1.5 py-1 font-medium text-slate-800"
                          />
                          <textarea
                            value={field.exampleValue}
                            onChange={(event) =>
                              setSuggestedExtractionFields((prev) =>
                                prev.map((item, index) =>
                                  index === idx ? { ...item, exampleValue: event.target.value } : item
                                )
                              )
                            }
                            className="mt-1 min-h-12 w-full rounded border border-slate-300 px-1.5 py-1 text-slate-600"
                          />
                          <button
                            type="button"
                            onClick={() => addSuggestedField(field)}
                            className="mt-1 rounded border border-slate-300 px-2 py-0.5 text-[11px] font-medium text-slate-700 hover:border-slate-400"
                          >
                            Add
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                <button
                  type="button"
                  onClick={captureSelectedText}
                  disabled={selectionLoading}
                  className="inline-flex w-full items-center justify-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {selectionLoading ? 'Analysing selection...' : 'Capture Selected Text (fallback)'}
                </button>
                <p className="text-xs text-slate-500">
                  Drag a box over the PDF like a screenshot tool. AI will infer field name/value pairs and keep a colored highlight.
                </p>
                <div className="min-h-0 flex-1 space-y-2 overflow-y-auto rounded border border-slate-200 bg-slate-50 p-2">
                  <p className="text-xs font-semibold text-slate-700">Field List (editable)</p>
                  {stagedExtractionFields.length === 0 ? (
                    <p className="text-xs text-slate-500">No fields captured yet.</p>
                  ) : (
                    stagedExtractionFields.map((field) => (
                      <div key={field.id} className="rounded border border-slate-200 bg-white p-2">
                        <label className="block text-[11px] font-medium text-slate-600">Field Name</label>
                        <input
                          value={field.fieldName}
                          onChange={(event) => updateStagedField(field.id, { fieldName: event.target.value })}
                          className="mt-1 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                        />
                        <label className="mt-2 block text-[11px] font-medium text-slate-600">Example Value</label>
                        <textarea
                          value={field.exampleValue}
                          onChange={(event) => updateStagedField(field.id, { exampleValue: event.target.value })}
                          className="mt-1 min-h-14 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                        />
                        <button
                          type="button"
                          onClick={() => removeStagedField(field.id)}
                          className="mt-1 rounded border border-rose-200 bg-rose-50 px-2 py-0.5 text-[11px] font-medium text-rose-700"
                        >
                          Remove
                        </button>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DocumentHub;
