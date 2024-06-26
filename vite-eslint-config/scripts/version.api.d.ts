export interface GetArtifactVersionResponse {
	count: number;
	value: Value[];
}

export interface Value {
	description: string;
	protocolMetadata: ProtocolMetadata;
	tags: any[];
	url: string;
	dependencies: Dependency[];
	_links: Links;
	sourceChain: any[];
	id: string;
	normalizedVersion: string;
	version: string;
	isLatest: boolean;
	isListed: boolean;
	storageId: string;
	views: View[];
	publishDate: string;
}

export interface Links {
	self: Feed;
	feed: Feed;
	package: Feed;
}

export interface Feed {
	href: string;
}

export interface Dependency {
	packageName: string;
	versionRange: string;
	group?: string;
}

export interface ProtocolMetadata {
	schemaVersion: number;
	data: Data;
}

export interface Data {
	author: Author;
	license: string;
	scripts: Record<string, string>;
	files: string[];
	main: string;
}

export interface Author {
	name: string;
	email: string;
	url: string;
}

export interface View {
	id: string;
	name: string;
	url: null;
	type: string;
}
