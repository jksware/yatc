/*
 *  Yet Another Tiger Compiler (YATC)
 *
 *  Copyright 2014 Damian Valdés Santiago, Juan Carlos Pujol Mainegra
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.  
 *
 */

grammar tiger;

options
{
	language = CSharp3;
    //language = Java;
    output = AST;
    k = 3;
}

tokens
{
	// para uso del tree adaptor
	ALIAS_DECL;
	ARRAY_ACCESS;
	ARRAY_DECL; 
	ARRAY_INST; 
	BREAK;
	EXPR_SEQ; 
	FIELD_ACCESS; 
	FIELD_ACCESS_TERMINAL; 
	FIELD_INST; 
	FILL_IN_TYPE; 
	FOR; 
	FUN_CALL; 
	FUN_DECL; 
	FUN_DECL_SEQ; 
	IF; 
	LET; 
	NEG;
	NIL; 
	PROGRAM;
	RECORD_DECL; 
	RECORD_INST; 
	TYPE; 
	TYPE_DECL; 
	TYPE_DECL_SEQ; 
	TYPE_FIELD; 
	VAR_ACCESS; 
	VAR_DECL; 
	WHILE; 

	// para vista
	ARGS_FIELDS;
	DECL_BLOCK;
	FIELDS_INST;
	FUN_TYPE_WRAPPER;
	PARAM_DECL;	
}

@lexer::header
{
	using System;
}

@lexer::namespace{YATC.Grammar}

@lexer::modifier{public}

@lexer::ctorModifier{public}

@lexer::members
{
    public override void ReportError(RecognitionException exc)
    {
		/* Abort on first error. */
		throw new ParsingException(GetErrorMessage(exc, TokenNames), exc);
    }
}

@parser::header
{ 
	using System;
}

@parser::namespace{YATC.Grammar}

@parser::modifier{public}

@parser::ctorModifier{public}

@parser::members
{
    public override void ReportError(RecognitionException exc) 
	{ 
		/* Abort on first error. */
		throw new ParsingException(GetErrorMessage(exc, TokenNames), exc);
    } 
}

fragment BEGIN_COMMENT 
	:	'/*'
	;
	
fragment END_COMMENT
	:	'*/'
	;

// binary ops
PLUS    : '+' ;
MINUS   : '-' ;
MULT    : '*' ;
DIV     : '/' ;

// binary comparison
EQ      : '=' ;
NOTEQ   : '<>' ;
GT      : '>' ;
GTEQ    : '>=' ;
LT      : '<' ;
LTEQ    : '<=' ;

// logical ops
AND     : '&' ;
OR      : '|' ;

// grouping symbols
LPAREN	: '(' ;
RPAREN	: ')' ;
LBRACKET  : '[' ;
RBRACKET  : ']' ;
LKEY    : '{' ;
RKEY    : '}' ;

// separators
COMMA	: ',' ;
SEMI	: ';' ;
COLON   : ':' ;
DOT     : '.' ;
fragment 
QUOTE   : '\"';

ASSIGN  : ':=' ;

// keywords
ARRAYKEY    : 'array';
BREAKKEY    : 'break';
DOKEY       : 'do';
ELSEKEY     : 'else';
ENDKEY      : 'end';
FORKEY      : 'for';
FUNCTIONKEY : 'function';
IFKEY       : 'if';
INKEY       : 'in';
INTKEY		: 'int';
LETKEY      : 'let';
NILKEY      : 'nil';
OFKEY       : 'of';
STRINGKEY	: 'string';
THENKEY     : 'then';
TOKEY       : 'to';
TYPEKEY     : 'type';
VARKEY      : 'var';
WHILEKEY    : 'while';

fragment
DIGIT
	:	'0'..'9'
	;

fragment 
LETTER
	:	'a'..'z'|'A'..'Z'
	;

fragment
ASCII_ESC
	:	'12' '0'..'7'
	|	'1' '0'..'1' '0'..'9'
	|	'0' '0'..'9' '0'..'9'
	;

INT
	:	DIGIT+
	;

ID  :	LETTER ( LETTER | DIGIT | '_' ) *
	;

WS
	:	( ' '|'\t'|'\r'|'\n' ) + 
	{$channel = Hidden;}
	//{$channel = HIDDEN;}
	;

fragment
ESC_SEQ 
	:	'\\' ( 'n' | 'r' | 't' | QUOTE | ASCII_ESC | WS '\\' )
	;

fragment
PRINTABLE_CHARACTER
	:	((' '..'!') | ('#'.. '[') | (']'..'~'))
	;

STRING : QUOTE ( ESC_SEQ | PRINTABLE_CHARACTER )* QUOTE ;

COMMENTARY
	:	BEGIN_COMMENT 
    		( options {greedy=false;} : . )* 
    		(  COMMENTARY ( options {greedy=false;} : . )* )* 
    	END_COMMENT
 		{$channel = Hidden;} 
    	//{$channel = HIDDEN;} 
	; 	

public a_program
	: expr ? EOF
		-> ^(PROGRAM expr ?) ;

expr
	:	(ID LBRACKET disjunction_expr RBRACKET OFKEY) => array_inst
	|	(lvalue ASSIGN) => assignment
	|	disjunction_expr	
	|	record_inst
	|	while_stat
	|	BREAKKEY
            -> BREAK
	;
	
disjunction_expr	
	:	conjunction_expr (OR^ conjunction_expr)*
	;

assignment
	:	lvalue ASSIGN^ 
			(	(ID LBRACKET disjunction_expr RBRACKET OFKEY) => array_inst 
			|	disjunction_expr 
			|	record_inst
			)
	;
	
record_inst
	:	ID LKEY field_inst_list? RKEY
			-> ^(RECORD_INST ID field_inst_list?)
	;

/*
	Queremos que en un for solo se puedan evaluar condiciones retornen valor,
	por ello que pedimos que la primera y segunda expresiones sean disjunction_expr,
	para evitar de que no devuelva por otros motivos.
*/
for_expr
	:	FORKEY type_id ASSIGN disjunction_expr TOKEY disjunction_expr DOKEY expr
			-> ^(FOR type_id disjunction_expr disjunction_expr expr)
	;

array_inst
	:	ID LBRACKET disjunction_expr RBRACKET OFKEY expr
			-> ^(ARRAY_INST ID disjunction_expr expr)
	;

conjunction_expr
	:	relational_expr (AND^ relational_expr)*
	;

/*
	El operador ? al final de la expresion le da la no asociatividad requerida 
	a los operadores de comparacion (notar que en los demas se usa * para 
	anidarlos en una lista). Esto es que no se permite a = b = c, pero si a = (b = c).
*/
relational_expr 
	:	arith_expr ((EQ | NOTEQ | GT | LT | GTEQ | LTEQ )^ arith_expr)?
	;

arith_expr 
	:	term_expr ((PLUS | MINUS)^ term_expr)*
	;

term_expr
	:	atom ((MULT | DIV)^ atom)*
	;

field_inst_list
	: 	record_field_inst ( COMMA record_field_inst )*
			-> ^(FIELDS_INST record_field_inst+)
	;

record_field_inst
	: 	ID EQ expr
			-> ^(FIELD_INST ID expr)
	;	

decl
	:	type_decl+
			-> ^(TYPE_DECL_SEQ type_decl+)
	|	var_decl
	|	fun_decl+
			-> ^(FUN_DECL_SEQ fun_decl+)
	;

type_decl
	:	TYPEKEY ID EQ type
			-> ^(TYPE_DECL ID type)
	;

var_decl
	:	VARKEY type_id COLON type_id ASSIGN expr	
			-> ^(VAR_DECL type_id type_id expr)
	|	VARKEY type_id ASSIGN expr
			-> ^(VAR_DECL type_id FILL_IN_TYPE expr)
	;

fun_decl
	:	FUNCTIONKEY type_id LPAREN type_fields? RPAREN (COLON type_id)? EQ expr
			-> ^(FUN_DECL type_id ^(PARAM_DECL type_fields?) ^(FUN_TYPE_WRAPPER type_id?) expr)
	;

type_id
	:	STRINGKEY
	|	INTKEY
	|	ID
	;

type
	:	type_id
            -> ^(ALIAS_DECL type_id)
	|	array_decl	
	|	record_decl
	;

record_decl
	:	LKEY type_fields? RKEY
			-> ^(RECORD_DECL type_fields?)
	;

array_decl
	:	ARRAYKEY OFKEY type_id
			-> ^(ARRAY_DECL type_id)
	;
	
type_fields
	:	type_field (COMMA type_field)*
			-> type_field+
	;

type_field
	:	type_id COLON type_id
			-> ^(TYPE_FIELD type_id type_id)
	;
	
atom
	:	MINUS atom
            -> ^(NEG atom)
	|	constant_value
	|	lvalue
	|	funcall
	|	if_then_expr
	|	for_expr
	|	let_expr
	|	LPAREN expr_seq RPAREN
			-> expr_seq
	;

constant_value
	:	STRING
	|	INT
	|	NILKEY
	;

if_then_expr
	:	IFKEY conditional=disjunction_expr THENKEY then_expr=expr (ELSEKEY else_expr=expr)?
			-> ^(IF $conditional $then_expr $else_expr?)
	;
	
while_stat
	:	WHILEKEY cond=disjunction_expr DOKEY do_expr=expr
			-> ^(WHILE $cond $do_expr)
	;

let_expr
	:	LETKEY decl+ INKEY expr_seq ENDKEY
			-> ^(LET ^(DECL_BLOCK decl+) expr_seq)
	;

lvalue 
	:	type_id lvalue_access? -> ^(VAR_ACCESS type_id lvalue_access?)
	;

lvalue_access
	:	DOT type_id lvalue_access?
            -> ^(FIELD_ACCESS type_id lvalue_access?)
	|	LBRACKET disjunction_expr RBRACKET lvalue_access?
			-> ^(ARRAY_ACCESS disjunction_expr lvalue_access?)
	;

funcall
	:	type_id LPAREN arg_list? RPAREN
			-> ^(FUN_CALL type_id arg_list?)
	;
	
expr_seq
	:	expr (SEMI expr)* 
			-> ^(EXPR_SEQ expr+)
    |   -> ^(EXPR_SEQ )
	;
	
arg_list
	:	expr (COMMA expr)*
			-> ^(ARGS_FIELDS expr+)
	;

