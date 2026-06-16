import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import type {
  DocumentWorkflowAuditRunDto,
  DocumentWorkflowRuleRunHistoryResponse,
} from '../services/documentHubService';

type TransposedRunHistory = {
  fieldNames: string[];
  runs: DocumentWorkflowAuditRunDto[];
};

type JsPdfWithAutoTable = jsPDF & {
  lastAutoTable: {
    finalY: number;
  };
};

const sanitizeFileName = (value: string): string =>
  value
    .trim()
    .replace(/[<>:"/\\|?*]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .slice(0, 80) || 'workflow-run-history';

const formatPdfRunDate = (run: DocumentWorkflowAuditRunDto): string => {
  const raw = run.completedDate ?? run.startedDate;
  if (!raw) return '—';
  return new Date(raw).toLocaleString();
};

const addPdfHeader = (doc: jsPDF, workflowName: string, viewLabel: string): void => {
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(14);
  doc.text('Workflow run history', 14, 16);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(10);
  doc.text(`${workflowName} (${viewLabel})`, 14, 22);
  doc.text(`Exported ${new Date().toLocaleString()}`, 14, 27);
};

const exportTransposedPdf = (
  doc: jsPDF,
  transposed: TransposedRunHistory,
): void => {
  const head = [['Run date', 'Subject', ...transposed.fieldNames]];
  const body = transposed.runs.map((run) => {
    const valuesByField = new Map(
      run.extractionSnapshots.map((snapshot) => [snapshot.fieldName, snapshot.fieldValue ?? '—'])
    );

    return [
      formatPdfRunDate(run),
      run.subject?.trim() || '(No subject)',
      ...transposed.fieldNames.map((fieldName) => valuesByField.get(fieldName) ?? '—'),
    ];
  });

  autoTable(doc, {
    startY: 32,
    head,
    body,
    styles: { fontSize: 7, cellPadding: 1.5, overflow: 'linebreak' },
    headStyles: { fillColor: [226, 232, 240], textColor: [30, 41, 59], fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [248, 250, 252] },
    margin: { left: 10, right: 10 },
    horizontalPageBreak: true,
    horizontalPageBreakRepeat: 2,
  });
};

const exportByRunPdf = (doc: jsPDF, runs: DocumentWorkflowAuditRunDto[]): void => {
  let startY = 32;
  const pageWidth = doc.internal.pageSize.getWidth();
  const pageHeight = doc.internal.pageSize.getHeight();
  const contentWidth = pageWidth - 28;

  for (let index = 0; index < runs.length; index += 1) {
    const run = runs[index];
    const runTitle = run.subject?.trim() || '(No subject)';
    const meta = [
      formatPdfRunDate(run),
      run.status,
      run.classificationLabel,
      run.classificationScore != null ? `score ${run.classificationScore.toFixed(2)}` : null,
    ]
      .filter(Boolean)
      .join(' · ');

    if (startY > pageHeight - 40) {
      doc.addPage();
      startY = 20;
    }

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    const titleLines = doc.splitTextToSize(runTitle, contentWidth);
    doc.text(titleLines, 14, startY);
    startY += titleLines.length * 5 + 2;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(71, 85, 105);
    const metaLines = doc.splitTextToSize(meta, contentWidth);
    doc.text(metaLines, 14, startY);
    startY += metaLines.length * 4 + 3;
    doc.setTextColor(0, 0, 0);

    if (run.errorMessage) {
      const errorLines = doc.splitTextToSize(`Error: ${run.errorMessage}`, contentWidth);
      doc.setTextColor(190, 18, 60);
      doc.text(errorLines, 14, startY);
      startY += errorLines.length * 4 + 2;
      doc.setTextColor(0, 0, 0);
    }

    if (run.summarisationText) {
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(9);
      doc.text('Summary', 14, startY);
      startY += 4;
      doc.setFont('helvetica', 'normal');
      const summaryLines = doc.splitTextToSize(run.summarisationText, contentWidth);
      doc.text(summaryLines, 14, startY);
      startY += summaryLines.length * 4 + 3;
    }

    if (run.extractionSnapshots.length > 0) {
      autoTable(doc, {
        startY,
        head: [['Field', 'Value']],
        body: run.extractionSnapshots.map((snapshot) => [
          snapshot.fieldName,
          snapshot.fieldValue ?? '—',
        ]),
        styles: { fontSize: 8, cellPadding: 2, overflow: 'linebreak' },
        headStyles: { fillColor: [226, 232, 240], textColor: [30, 41, 59], fontStyle: 'bold' },
        columnStyles: { 0: { cellWidth: 55 }, 1: { cellWidth: 'auto' } },
        margin: { left: 14, right: 14 },
      });
      startY = (doc as JsPdfWithAutoTable).lastAutoTable.finalY + 8;
    } else {
      doc.setFontSize(8);
      doc.setTextColor(100, 116, 139);
      doc.text('No extracted fields for this run.', 14, startY);
      startY += 6;
      doc.setTextColor(0, 0, 0);
    }

    if (index < runs.length - 1) {
      doc.setDrawColor(226, 232, 240);
      doc.line(14, startY, pageWidth - 14, startY);
      startY += 6;
    }
  }
};

export const canExportWorkflowRunHistoryPdf = (
  viewMode: 'byRun' | 'transposed',
  runs: DocumentWorkflowAuditRunDto[],
  transposed: TransposedRunHistory,
): boolean => {
  if (runs.length === 0) return false;
  if (viewMode === 'transposed') return transposed.fieldNames.length > 0;
  return true;
};

export const exportWorkflowRunHistoryPdf = (
  history: DocumentWorkflowRuleRunHistoryResponse,
  viewMode: 'byRun' | 'transposed',
  transposed: TransposedRunHistory,
): void => {
  const viewLabel = viewMode === 'byRun' ? 'By run' : 'Transposed';
  const doc = new jsPDF({
    orientation: viewMode === 'transposed' ? 'landscape' : 'portrait',
    unit: 'mm',
    format: 'a4',
  });

  addPdfHeader(doc, history.workflowName, viewLabel);

  if (viewMode === 'transposed') {
    exportTransposedPdf(doc, transposed);
  } else {
    exportByRunPdf(doc, history.runs);
  }

  const fileName = `${sanitizeFileName(history.workflowName)}-run-history-${viewMode}.pdf`;
  doc.save(fileName);
};
