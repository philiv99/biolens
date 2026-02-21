/** User from CopilotSdk.Api */
export interface User {
  id: string;
  username: string;
  email: string;
  displayName: string;
  role: 'Player' | 'Creator' | 'Admin';
  avatarType: string;
  avatarData: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt: string | null;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  user: User;
}

/** Parsed document summary (list view) */
export interface DocumentSummary {
  id: string;
  fileName: string;
  subjectNames: string[];
  uploadedAt: string;
  totalParagraphs: number;
  hasExtraction: boolean;
}

/** Full parsed document with paragraphs */
export interface ParsedDocument {
  id: string;
  fileName: string;
  subjectNames: string[];
  uploadedBy: string;
  uploadedAt: string;
  paragraphs: DocumentParagraph[];
  totalParagraphs: number;
}

export interface DocumentParagraph {
  index: number;
  text: string;
  style: string;
}

/** Source reference linking extraction data back to a paragraph */
export interface SourceReference {
  paragraphIndex: number;
  snippet: string;
}

/** Extraction category items */
export interface ExtractedPerson {
  id: string;
  name: string;
  relationship: string;
  description: string;
  sourceRefs: SourceReference[];
}

export interface ExtractedEvent {
  id: string;
  title: string;
  date: string;
  description: string;
  sourceRefs: SourceReference[];
}

export interface ExtractedPlace {
  id: string;
  name: string;
  context: string;
  description: string;
  sourceRefs: SourceReference[];
}

export interface ExtractedConversation {
  id: string;
  participants: string;
  topic: string;
  summary: string;
  sourceRefs: SourceReference[];
}

export interface ExtractedThought {
  id: string;
  topic: string;
  content: string;
  attribution: string;
  sourceRefs: SourceReference[];
}

export interface ExtractionCategories {
  people: ExtractedPerson[];
  events: ExtractedEvent[];
  places: ExtractedPlace[];
  conversations: ExtractedConversation[];
  thoughts: ExtractedThought[];
}

/** Full extraction result */
export interface BiographicalExtraction {
  documentId: string;
  extractedAt: string;
  subjectNames: string[];
  categories: ExtractionCategories;
  summary: string;
}

/** Category key type */
export type CategoryKey = keyof ExtractionCategories;

/** Generic extracted item (union of all item types) */
export type ExtractedItem =
  | ExtractedPerson
  | ExtractedEvent
  | ExtractedPlace
  | ExtractedConversation
  | ExtractedThought;
